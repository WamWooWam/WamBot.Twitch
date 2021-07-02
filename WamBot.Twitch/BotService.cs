using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Services;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data;
using TwitchLib.Api;
using System.Diagnostics;

namespace WamBot.Twitch
{
    internal class BotService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BotService> _logger;
        private readonly TwitchClient _client;
        private readonly CommandRegistry _registry;
        private readonly LiveStreamMonitorService _monitorService;
        private readonly string _prefix = "!";
        private readonly TwitchAPI _api;
        private bool _stop;

        public BotService(
            TwitchAPI api,
            IServiceProvider services,
            IConfiguration configuration,
            ILogger<BotService> logger,
            TwitchClient client,
            CommandRegistry registry,
            LiveStreamMonitorService monitorService)
        {
            _services = services;
            _configuration = configuration;
            _prefix = configuration["Twitch:Prefix"] ?? _prefix;

            _api = api;
            _logger = logger;
            _client = client;
            _registry = registry;
            _monitorService = monitorService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await UpdateDatabaseAsync();

            var asm = Assembly.GetExecutingAssembly();
            var types = asm.GetTypes()
                           .Where(t => t != typeof(CommandModule) && typeof(CommandModule).IsAssignableFrom(t) && !t.IsNested);

            foreach (var type in types)
            {
                _logger.LogDebug("Registering command group from {Group}", type.FullName);
                _registry.RegisterCommands(type);
            }

            _client.AutoReListenOnException = true;
            _client.OnConnected += OnConnected;
            _client.OnDisconnected += OnDisconnected;
            _client.OnReconnected += OnReconnected; ;
            _client.OnMessageReceived += OnMessage;
            _client.OnJoinedChannel += OnJoinedChannel;
            _client.OnLeftChannel += OnLeftChannel;
            _client.Connect();
        }

        private async Task UpdateDatabaseAsync()
        {
            using var scope = _services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            await dbContext.Database.MigrateAsync();

            var channels = await dbContext.DbChannels.Select(c => c.Name).ToListAsync();
            channels.Add("wambot_");

            _monitorService.SetChannelsByName(channels);
            _monitorService.Start();
        }

        private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            _logger.LogInformation("Joined channel {Channel}", e.Channel);
        }

        private void OnLeftChannel(object sender, OnLeftChannelArgs e)
        {
            _logger.LogInformation("Left channel {Channel}", e.Channel);
        }

        private void OnMessage(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.IsMe || !e.ChatMessage.Message.StartsWith(_prefix))
                return;

            var timestamp = Stopwatch.GetTimestamp();
            var message = e.ChatMessage.Message.Substring(_prefix.Length);
            if (!_registry.Lookup(message, out var command, out var args))
                return;

            var ctx = new CommandContext(_client, e.ChatMessage, _services, args);
            _ = Task.Run(async () =>
            {
                try
                {
                    await command.Run(ctx, ctx.Arguments);
                    _logger.LogInformation("Command \"{Name}\" was run by {User} in {Channel} ({Time:N2}ms)", command.Name, ctx.Message.Username, ctx.Message.Channel, Extensions.TimestampToMilliseconds(timestamp));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Command \"{Name}\" failed! {User} in {Channel} ({Time:N2}ms)", command.Name, ctx.Message.Username, ctx.Message.Channel, Extensions.TimestampToMilliseconds(timestamp));
                }
            });
        }

        private void OnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation("Connected to Twitch as {Username}", e.BotUsername);
            _client.JoinChannel(e.BotUsername);

            using var scope = _services.CreateScope();
            using var database = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            foreach (var channel in database.DbChannels)
            {
                _logger.LogDebug("Joining {Channel}", channel.Name);
                _client.JoinChannel(channel.Name);
            }
        }

        private void OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            _logger.LogWarning("Disconnected from Twitch!");


            try
            {
                if (!_stop)
                    _client.Reconnect();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reconnect to twitch!");
            }
        }

        private void OnReconnected(object sender, OnReconnectedEventArgs e)
        {
            _logger.LogWarning("Reconnected to Twitch!");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _stop = true;
            _client.Disconnect();
            return Task.CompletedTask;
        }
    }
}

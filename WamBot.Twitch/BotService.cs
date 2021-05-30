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
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data;
using WamBot.Twitch.Services;

namespace WamBot.Twitch
{
    internal class BotService : IHostedService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BotService> _logger;
        private readonly TwitchClient _client;
        private readonly CommandRegistry _registry;
        private string _prefix = "!";

        public BotService(
            IServiceProvider services,
            IConfiguration configuration,
            ILogger<BotService> logger,
            TwitchClient client,
            CommandRegistry registry)
        {
            _services = services;
            _configuration = configuration;
            _prefix = configuration["Twitch:Prefix"] ?? _prefix;

            _logger = logger;
            _client = client;
            _registry = registry;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var asm = Assembly.GetExecutingAssembly();
            var types = asm.GetTypes()
                           .Where(t => t != typeof(CommandModule) && typeof(CommandModule).IsAssignableFrom(t) && !t.IsNested);

            foreach (var type in types)
            {
                _logger.LogDebug("Registering command group from {Group}", type.FullName);
                _registry.RegisterCommands(type);
            }

            _client.OnConnected += OnConnected;
            _client.OnDisconnected += OnDisconnected;
            _client.OnMessageReceived += OnMessage;
            _client.OnJoinedChannel += OnJoinedChannel;
            _client.OnLeftChannel += OnLeftChannel;
            _client.Connect();

            return Task.CompletedTask;
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

            var message = e.ChatMessage.Message.Substring(_prefix.Length);
            if (!_registry.Lookup(message, out var command, out var args))
                return;

            var ctx = new CommandContext(_client, e.ChatMessage, _services, args);
            _ = Task.Run(async () =>
            {
                try
                {
                    await command.Run(ctx, ctx.Arguments);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured in a command!");
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
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Disconnect();
            return Task.CompletedTask;
        }
    }
}

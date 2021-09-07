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
        private readonly LiveStreamMonitorService _monitorService;
        private readonly TwitchAPI _api;
        private bool _stop;

        private readonly string[] _prefixes = new[] { "!", "w;" };

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

            var prefixes = configuration["Twitch:Prefix"];
            if (!string.IsNullOrWhiteSpace(prefixes))
                _prefixes = prefixes.Split(' ');

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
            _client.OnReconnected += OnReconnected; 
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

            //await RefreshUsers(dbContext);
            await RefreshChannels(dbContext);

            await dbContext.SaveChangesAsync();

            var channels = new List<string> { "wambot_" };
            using var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            foreach (var channel in await dbContext.DbChannels.ToListAsync())
            {
                var user = await userService.GetTwitchUserAsync(channel.Id);
                channels.Add(user.Name);
            }

            _monitorService.SetChannelsByName(channels);
            _monitorService.Start();
        }

        private async Task RefreshUsers(BotDbContext dbContext)
        {
            var i = 0;
            var users = await dbContext.DbUsers.ToListAsync();
            foreach (var userGroup in users.Split(100))
            {
                var userResponse = (await _api.Helix.Users.GetUsersAsync(ids: userGroup.Select(u => u.Id.ToString()).ToList())).Users;
                foreach (var user in userGroup)
                {
                    var twitchUser = userResponse.FirstOrDefault(u => u.Id == user.Id.ToString());
                    if (twitchUser == null)
                    {
                        i++;
                        dbContext.DbUsers.Remove(user);
                        dbContext.DbChannelUsers.RemoveRange(dbContext.DbChannelUsers.Where(u => u.UserId == user.Id));
                        continue;
                    }

                    user.Name = twitchUser.Login;
                }
            }

            _logger.Log(LogLevel.Information, "Removed {Count} dead users!", i);
        }

        private async Task RefreshChannels(BotDbContext dbContext)
        {
            var i = 0;
            var channels = await dbContext.DbChannels.ToListAsync();
            foreach (var channelGroup in channels.Split(100))
            {
                var channelResponse = (await _api.Helix.Users.GetUsersAsync(ids: channelGroup.Select(u => u.Id.ToString()).ToList())).Users;
                foreach (var channel in channelGroup)
                {
                    var twitchUser = channelResponse.FirstOrDefault(u => u.Id == channel.Id.ToString());
                    if (twitchUser == null)
                    {
                        i++;
                        dbContext.DbChannels.Remove(channel);
                        dbContext.DbChannelUsers.RemoveRange(dbContext.DbChannelUsers.Where(u => u.ChannelId == channel.Id));
                        continue;
                    }

                    channel.Name = twitchUser.Login;
                }
            }

            _logger.Log(LogLevel.Information, "Removed {Count} dead channels!", i);
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
            if (e.ChatMessage.IsMe)
                return;

            string prefix = null;
            foreach (var item in _prefixes)
            {
                if (e.ChatMessage.Message.StartsWith(item))
                {
                    prefix = item;
                    break;
                }
            }

            if (prefix == null) return;

            var timestamp = Stopwatch.GetTimestamp();
            var message = e.ChatMessage.Message.Substring(prefix.Length);
            if (!_registry.Lookup(message, out var command, out var args))
                return;

            var ctx = new CommandContext(_client, e.ChatMessage, _services, args, prefix);
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

        private async void OnConnected(object sender, OnConnectedArgs e)
        {
            _logger.LogInformation("Connected to Twitch as {Username}", e.BotUsername);
            _client.JoinChannel(e.BotUsername);

            using var scope = _services.CreateScope();
            using var database = scope.ServiceProvider.GetRequiredService<BotDbContext>();
            using var userService = scope.ServiceProvider.GetRequiredService<UserService>();
            foreach (var channel in database.DbChannels)
            {
                var user = await userService.GetTwitchUserAsync(channel.Id);

                _logger.LogDebug("Joining {Channel}", user.Name);
                _client.JoinChannel(user.Name);
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

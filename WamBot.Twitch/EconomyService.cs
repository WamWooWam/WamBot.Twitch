using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using WamBot.Twitch.Data;
using WamBot.Twitch.Services;

namespace WamBot.Twitch
{
    public class EconomyService : IHostedService
    {
        private class ChannelState
        {
            public ChannelState()
            {
                IsLive = false;
                ActiveUsers = new ConcurrentDictionary<string, ActiveUser>();
            }

            public bool IsLive { get; set; }
            public DateTimeOffset StreamStarted { get; set; }
            public ConcurrentDictionary<string, ActiveUser> ActiveUsers { get; set; }
            public string StreamId { get; internal set; }
        }

        private class ActiveUser
        {
            public bool IsActive { get; set; }
            public DateTimeOffset LastSeen { get; set; }
        }

        private readonly ILogger<EconomyService> _logger;
        private readonly IServiceProvider _services;
        private readonly TwitchAPI _twitchAPI;
        private readonly TwitchClient _twitchClient;
        private readonly UserService _userService;
        private readonly LiveStreamMonitorService _liveStreamMonitor;
        private readonly ConcurrentDictionary<string, ChannelState> _channelStateStore;
        private readonly Timer _payoutTimer;

        private readonly ConcurrentQueue<Func<Task>> _taskQueue;
        private Task _queueTask;

        private const decimal PER_TICK_BONUS = 0.50m;
        private const decimal TUNE_IN_BONUS = 100.0m;

        public EconomyService(
            ILogger<EconomyService> logger,
            IConfiguration configuration,
            IServiceProvider services,
            TwitchClient twitchClient,
            TwitchAPI twitchAPI,
            UserService userService,
            LiveStreamMonitorService liveStreamMonitor)
        {
            _logger = logger;
            _services = services;
            _twitchAPI = twitchAPI;
            _twitchClient = twitchClient;
            _liveStreamMonitor = liveStreamMonitor;
            _userService = userService;

            _taskQueue = new ConcurrentQueue<Func<Task>>();
            _channelStateStore = new ConcurrentDictionary<string, ChannelState>();
            _payoutTimer = new Timer(OnPayoutTick, null, 0, 60_000);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _twitchClient.OnExistingUsersDetected += OnExistingUsersDetected;
            _twitchClient.OnUserJoined += OnUserJoined;
            _twitchClient.OnUserLeft += OnUserLeft;

            _liveStreamMonitor.OnStreamOnline += OnStreamOnline;
            _liveStreamMonitor.OnStreamOffline += OnStreamOffline;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _twitchClient.OnExistingUsersDetected -= OnExistingUsersDetected;
            _twitchClient.OnUserJoined -= OnUserJoined;
            _twitchClient.OnUserLeft -= OnUserLeft;

            _liveStreamMonitor.OnStreamOnline -= OnStreamOnline;
            _liveStreamMonitor.OnStreamOffline -= OnStreamOffline;
            return Task.CompletedTask;
        }

        private void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            var activeUserStore = GetChannelState(e.Channel);
            activeUserStore.IsLive = true;
            activeUserStore.StreamStarted = e.Stream.StartedAt;
            activeUserStore.StreamId = e.Stream.Id;
        }

        private void OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            var activeUserStore = GetChannelState(e.Channel);
            activeUserStore.IsLive = false;
            activeUserStore.StreamId = null;
        }

        private void OnExistingUsersDetected(object sender, OnExistingUsersDetectedArgs e) => QueueTask(async () =>
        {
            using var scope = _services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var channelState = GetChannelState(e.Channel);
            foreach (var user in e.Users)
            {
                var dbUser = await _userService.GetOrCreateChannelUserAsync(e.Channel, user);
                if (channelState.StreamId != null && dbUser.LastStreamId != channelState.StreamId)
                {
                    dbUser.Balance += TUNE_IN_BONUS;
                    dbUser.LastStreamId = channelState.StreamId;

                    _logger.LogDebug("Giving {User} tune in bonus!", user);
                }

                var activeUser = channelState.ActiveUsers.GetOrAdd(user, (s) => new ActiveUser() { IsActive = true, LastSeen = DateTimeOffset.Now });

                activeUser.IsActive = true;
                activeUser.LastSeen = DateTimeOffset.Now;
            }

            await dbContext.SaveChangesAsync();
        });

        private void OnUserJoined(object sender, OnUserJoinedArgs e) => QueueTask(async () =>
        {
            using var scope = _services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var channelState = GetChannelState(e.Channel);
            var dbUser = await _userService.GetOrCreateChannelUserAsync(e.Channel, e.Username);
            if (channelState.StreamId != null && dbUser.LastStreamId != channelState.StreamId)
            {
                dbUser.Balance += TUNE_IN_BONUS;
                dbUser.LastStreamId = channelState.StreamId;

                _logger.LogDebug("Giving {User} tune in bonus!", e.Username);
            }

            var activeUser = channelState.ActiveUsers.GetOrAdd(e.Username, (s) => new ActiveUser() { IsActive = true, LastSeen = DateTimeOffset.Now });
            activeUser.IsActive = true;
            activeUser.LastSeen = DateTimeOffset.Now;

            await dbContext.SaveChangesAsync();
        });

        private void OnUserLeft(object sender, OnUserLeftArgs e)
        {
            var activeUserStore = GetChannelState(e.Channel);
            var activeUser = activeUserStore.ActiveUsers.GetOrAdd(e.Username, (s) => new ActiveUser() { IsActive = false, LastSeen = DateTimeOffset.Now });
            activeUser.IsActive = false;
            activeUser.LastSeen = DateTimeOffset.Now;
        }

        private void OnPayoutTick(object _) => QueueTask(async () =>
        {
            var stamp = Stopwatch.GetTimestamp();
            using var scope = _services.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();

            var i = 0;
            var j = 0;
            foreach (var state in this._channelStateStore)
            {
                if (!state.Value.IsLive)
                    continue;

                foreach (var user in state.Value.ActiveUsers)
                {
                    if (!user.Value.IsActive)
                        continue;
                    
                    j++;

                    var dbUser = await _userService.GetOrCreateChannelUserAsync(state.Key, user.Key, false);
                    if (dbUser != null && dbUser.LastStreamId == state.Value.StreamId)
                    {
                        dbUser.Balance += PER_TICK_BONUS;
                        i++;
                        _logger.LogDebug("Giving {User} per tick bonus!", user.Key);
                    }
                }
            }


            if (i > 0)
            {
                await dbContext.SaveChangesAsync();
                _logger.LogInformation("Gave {Num1}/{Num2} users per tick bonus ({Time:N2}ms)", i, j, Extensions.TimestampToMilliseconds(stamp));
            }
        });

        private ChannelState GetChannelState(string channelName)
            => _channelStateStore.TryGetValue(channelName, out var store) ? store : _channelStateStore[channelName] = new ChannelState();

        private async Task RunQueueAsync()
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                try
                {
                    await task();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occured in the task queue!");
                }
            }
        }

        private void QueueTask(Func<Task> task)
        {
            _taskQueue.Enqueue(task);

            if (_queueTask == null || _queueTask.IsCompleted)
                _queueTask = RunQueueAsync();
        }
    }
}

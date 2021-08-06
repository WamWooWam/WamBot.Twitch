using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Streams.GetStreams;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using WamBot.Twitch.Data;

namespace WamBot.Twitch
{
    public class WelcomeService : IHostedService
    {
        private ILogger<WelcomeService> _logger;
        private IServiceProvider _services;
        private TwitchAPI _twitchAPI;
        private TwitchClient _twitchClient;
        private LiveStreamMonitorService _liveStreamMonitor;
        private ConcurrentDictionary<string, Stream> _welcomeMessageQueue;
        private RandomList<string> _welcomeLines;

        public WelcomeService(
            ILogger<WelcomeService> logger,
            IServiceProvider services,
            TwitchClient twitchClient,
            TwitchAPI twitchAPI,
            LiveStreamMonitorService liveStreamMonitor)
        {
            _logger = logger;
            _services = services;
            _twitchAPI = twitchAPI;
            _twitchClient = twitchClient;
            _liveStreamMonitor = liveStreamMonitor;
            _welcomeMessageQueue = new ConcurrentDictionary<string, Stream>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _welcomeLines = new RandomList<string>(await System.IO.File.ReadAllLinesAsync("welcome-lines.txt", cancellationToken));

            _twitchClient.OnJoinedChannel += OnJoinedChannel;
            _liveStreamMonitor.OnStreamOnline += OnStreamOnline;
            await _liveStreamMonitor.UpdateLiveStreamersAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _twitchClient.OnJoinedChannel -= OnJoinedChannel;
            _liveStreamMonitor.OnStreamOnline -= OnStreamOnline;
            return Task.CompletedTask;
        }

        private void OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            if (_welcomeMessageQueue.TryRemove(e.Channel, out var stream))
                SendChannelWelcomeMessage(e.Channel, stream);
        }

        private void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            try
            {
                using var scope = _services.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();

                var dbChannel = db.DbChannels.Find(long.Parse(e.Stream.UserId));
                if (dbChannel == null)
                {
                    dbChannel = new DbChannel() { Name = e.Channel, Id = long.Parse(e.Stream.UserId) };
                    db.DbChannels.Add(dbChannel);
                }

                if (dbChannel.LastStreamId != e.Stream.Id)
                {
                    if (_twitchClient.JoinedChannels.Any(c => c.Channel == e.Channel))
                        SendChannelWelcomeMessage(e.Channel, e.Stream);
                    else
                        _welcomeMessageQueue[e.Channel] = e.Stream;
                }

                dbChannel.LastStreamId = e.Stream.Id;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occured in OnStreamOnline!");
            }
        }

        private void SendChannelWelcomeMessage(string channel, Stream stream)
        {
            var line = _welcomeLines.Next();
            _twitchClient.SendMessage(channel, string.Format(line, stream.UserName, stream.Title, stream.GameName));
        }
    }
}

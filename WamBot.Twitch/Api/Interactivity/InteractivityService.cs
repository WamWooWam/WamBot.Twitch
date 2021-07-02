using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using WamBot.Twitch.Api;

namespace WamBot.Twitch.Interactivity
{
    public class InteractivityService
    {
        private record InteractivityContext(Func<ChatMessage, bool?> Predecate, TaskCompletionSource<ChatMessage> TaskCompletionSource);

        private readonly TwitchClient _twitchClient;
        private readonly TwitchAPI _twitchApi;
        private readonly CommandContext _ctx;
        private readonly TimeSpan _defaultTimeout = TimeSpan.FromMinutes(1);

        public InteractivityService(TwitchClient twitchClient, TwitchAPI twitchAPI, CommandContext ctx)
        {
            _twitchClient = twitchClient;
            _twitchApi = twitchAPI;
            _ctx = ctx;
        }

        public async Task<bool> WaitForYesNoAsync(string username = null, string yes = "!accept", string no = "!deny", TimeSpan? timeout = null)
        {
            username = username ?? _ctx.Message.Username;

            bool? YesNoCancelPredecate(ChatMessage m)
            {
                var content = m.Message.Trim().ToLowerInvariant();
                if (content.StartsWith(yes)) return true;
                if (content.StartsWith(no)) return null;
                return false;
            }

            var message = await WaitForMessageAsync(username, YesNoCancelPredecate, timeout);
            return message != null;
        }

        public Task<ChatMessage> WaitForMessageAsync(string username, Func<ChatMessage, bool?> predecate, TimeSpan? timeout = null)
        {
            bool? UsernamePredecate(ChatMessage m)
            {
                if (m.Username != username)
                    return false;

                return predecate(m);
            }

            return WaitForMessageAsync(UsernamePredecate, timeout);
        }

        public async Task<ChatMessage> WaitForMessageAsync(Func<ChatMessage, bool?> predecate, TimeSpan? timeout = null)
        {
            var tcs = new TaskCompletionSource<ChatMessage>();
            var cts = new CancellationTokenSource(timeout ?? _defaultTimeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            var context = new InteractivityContext(predecate, tcs);
            void OnMessageRecieved(object sender, OnMessageReceivedArgs e)
            {
                if (_ctx.Message.Channel != e.ChatMessage.Channel)
                    return;

                var result = context.Predecate(e.ChatMessage);

                if (result == true)
                    context.TaskCompletionSource.SetResult(e.ChatMessage);

                if (result == null)
                    context.TaskCompletionSource.TrySetCanceled();
            }

            _twitchClient.OnMessageReceived += OnMessageRecieved;

            try
            {
                return await tcs.Task;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
            finally
            {
                _twitchClient.OnMessageReceived -= OnMessageRecieved;
            }
        }
    }
}

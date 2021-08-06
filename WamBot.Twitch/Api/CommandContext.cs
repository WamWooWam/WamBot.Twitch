using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using WamBot.Twitch.Interactivity;

namespace WamBot.Twitch.Api
{
    public class CommandContext
    {
        private Lazy<InteractivityService> _interactivityLazy;

        internal CommandContext(TwitchClient client, ChatMessage chatMessage, IServiceProvider services, string[] args, string prefix)
        {
            _interactivityLazy = new Lazy<InteractivityService>(() => ActivatorUtilities.CreateInstance<InteractivityService>(services, this));

            Client = client;
            Message = chatMessage;
            Arguments = args;
            Services = services;
            Prefix = prefix;
        }

        public TwitchClient Client { get; }
        public ChatMessage Message { get; }
        public string Prefix { get; }
        public string[] Arguments { get; }
        public string Content => Message.Message;
        public IServiceProvider Services { get; }

        public InteractivityService Interactivity =>
            _interactivityLazy.Value;

        public void Reply(string message, bool asReply = false)
        {
            if (asReply)
            {
                Client.SendReply(Message.Channel, Message.Id, message);
            }
            else
            {
                Client.SendMessage(Message.Channel, message);
            }
        }
    }
}

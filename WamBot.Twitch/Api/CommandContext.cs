using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace WamBot.Twitch.Api
{
    public class CommandContext
    {
        internal CommandContext(TwitchClient client, ChatMessage chatMessage, IServiceProvider services, string[] args)
        {
            Client = client;
            Message = chatMessage;
            Arguments = args;
            Services = services;
        }

        public TwitchClient Client { get; }
        public ChatMessage Message { get; }
        public string[] Arguments { get; }
        public string Content => Message.Message;
        public IServiceProvider Services { get; }

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

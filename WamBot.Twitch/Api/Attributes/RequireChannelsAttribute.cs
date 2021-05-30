using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Api
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class RequireChannelsAttribute : ChecksAttribute
    {
        public RequireChannelsAttribute(params string[] channels)
        {
            this.Channels = channels;
        }

        public string[] Channels { get; }

        public override bool DoCheck(CommandContext ctx)
        {
            if (Channels.Contains(ctx.Message.Channel.ToLowerInvariant()))
                return true;
            return false;
        }
    }
}

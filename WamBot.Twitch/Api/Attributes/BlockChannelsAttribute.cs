using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Api
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class BlockChannelsAttribute : ChecksAttribute
    {
        public BlockChannelsAttribute(params string[] channels)
        {
            this.Channels = channels;
        }

        public string[] Channels { get; }

        public string[] WithPrefixes { get; set; }

        public override bool DoCheck(CommandContext ctx)
        {
            if (Channels.Contains(ctx.Message.Channel.ToLowerInvariant()) && (WithPrefixes == null || WithPrefixes.Contains(ctx.Prefix)))
                return false;
            return true;
        }
    }
}

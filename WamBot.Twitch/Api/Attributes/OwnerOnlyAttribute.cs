using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Twitch.Api
{
    /// <summary>
    /// Defines a command as only runnable by the bot's owner.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    class OwnerOnlyAttribute : ChecksAttribute
    {
        public override bool DoCheck(CommandContext ctx)
        {
            return ctx.Message.UserId == "50030128";
        }
    }
}

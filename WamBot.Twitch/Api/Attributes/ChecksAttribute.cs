using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Api
{
    public abstract class ChecksAttribute : Attribute
    {
        public abstract bool DoCheck(CommandContext ctx);
    }
}

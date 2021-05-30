using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Api
{
    public interface IParamConverter<T>
    {
        Task<object> Convert(string arg, CommandContext ctx);
    }
}

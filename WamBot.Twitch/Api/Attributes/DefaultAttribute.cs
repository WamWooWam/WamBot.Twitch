using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Api
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class DefaultAttribute : Attribute
    {

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace WamBot.Twitch.Api
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class CooldownAttribute : Attribute
    {
        public CooldownAttribute(double seconds)
        {
            this.Cooldown = TimeSpan.FromSeconds(seconds);
        }

        public TimeSpan Cooldown { get; }
        public bool PerUser { get; set; } = false;
    }
}

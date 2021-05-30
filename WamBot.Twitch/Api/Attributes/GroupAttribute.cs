using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WamBot.Twitch.Api
{
    /// <summary>
    /// Defines a method as a command that can be invoked by bot users.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class GroupAttribute : Attribute
    {
        internal string Name { get; private set; }
        internal string Description { get; private set; }
        internal string[] Aliases { get; private set; }

        public GroupAttribute(string name, string description, params string[] aliases)
        {
            Name = name;
            Description = description;
            Aliases = aliases;
        }

        public string ExtendedDescription { get; set; }
    }
}

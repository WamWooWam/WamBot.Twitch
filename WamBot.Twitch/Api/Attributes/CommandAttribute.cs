using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WamBot.Twitch.Api
{
    /// <summary>
    /// Defines a method as a command that can be invoked by bot users.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        internal string Name { get; private set; }
        internal string Description { get; private set; }
        internal string[] Aliases { get; private set; }

        public CommandAttribute(string name, string description, params string[] aliases)
        {
            Name = name;
            Description = description;

            if (!name.Any(char.IsWhiteSpace))
            {
                Aliases = aliases.Prepend(name.ToLowerInvariant()).Distinct().ToArray();
            }
            else
            {
                Aliases = aliases;
            }
        }

        public string ExtendedDescription { get; set; }
    }
}

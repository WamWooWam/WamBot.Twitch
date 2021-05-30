using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WamBot.Twitch.Api
{
    internal class CommandGroup
    {
        private Type _type;
        public CommandGroup(Type t)
        {
            _type = t;
            Aliases = Array.Empty<string>();

            var groupAttribute = t.GetCustomAttributes(true).OfType<GroupAttribute>().FirstOrDefault();
            if (groupAttribute != null)
            {
                Name = groupAttribute.Name;
                Description = groupAttribute.Description;
                Aliases = groupAttribute.Aliases;
            }
        }

        public string Name { get; }
        public string Description { get; }
        public string[] Aliases { get; }

        public Command[] GetCommands()
        {
            return Extensions.GetCommands(_type).ToArray();
        }
    }
}

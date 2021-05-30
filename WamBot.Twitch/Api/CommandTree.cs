using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace WamBot.Twitch.Api
{
    internal class CommandTreeNode
    {
        private Dictionary<string, CommandTreeNode> _children;

        public Command Command { get; private set; }
        public CommandGroup Group { get; }
        public CommandTreeNode Parent { get; }
        public IReadOnlyDictionary<string, CommandTreeNode> Children => _children;

        public CommandTreeNode()
        {
            _children = new Dictionary<string, CommandTreeNode>();
        }

        private CommandTreeNode(CommandGroup group, CommandTreeNode parent = null) : this()
        {
            Parent = parent;
            Group = group;
        }

        private CommandTreeNode(Command command, CommandTreeNode parent = null) : this()
        {
            Command = command;
            Parent = parent;
            Group = parent?.Group;
        }

        public void Add(Command command)
        {
            if (command.Default)
            {
                if (this.Command != null)
                    throw new InvalidOperationException("Can't have multiple default commands!");

                this.Command = command;
            }

            foreach (var item in command.Aliases)
                _children.Add(item.ToLowerInvariant(), new CommandTreeNode(command, this));
        }

        public void Add(Type t)
            => Add(t, false);

        private void Add(Type t, bool isSubGroup)
        {
            var group = new CommandGroup(t);
            var node = group.Aliases.Any() ? new CommandTreeNode(group, this) : this;
            if (node == this && isSubGroup)
                throw new InvalidOperationException("Nested command groups must have an alias!");

            foreach (var command in group.GetCommands())
                node.Add(command);

            var nestedTypes = t.GetNestedTypes().Where(t => typeof(CommandModule).IsAssignableFrom(t));
            foreach (var subGroup in nestedTypes)
                node.Add(subGroup, true);

            foreach (var alias in group.Aliases)
                _children.Add(alias.ToLowerInvariant(), node);
        }
    }
}

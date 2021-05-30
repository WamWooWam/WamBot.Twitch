using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WamBot.Twitch;
using WamBot.Twitch.Api;
using WamBot.Twitch.Commands;
using Microsoft.Extensions.Logging;

namespace WamBot.Twitch.Services
{
    internal class CommandRegistry
    {
        private ILogger<CommandRegistry> _logger;
        private CommandTreeNode _root;

        public CommandRegistry(ILogger<CommandRegistry> logger)
        {
            _logger = logger;
            _root = new CommandTreeNode();
        }

        public void RegisterCommands(Type t)
        {
            _root.Add(t);
            _logger.LogDebug("Registered commands from {Type}!", t.FullName);
        }

        public bool Lookup(string commandText, out Command command, out string[] args)
        {
            command = null;
            args = null;

            var parsedArgs = commandText.SplitCommandLine().ToArray();
            if (parsedArgs.Length == 0)
                return false;

            Lookup(parsedArgs, out var node, out args);
            command = node.Command;
            return command != null;
        }

        public void Lookup(string[] parsedArgs, out CommandTreeNode node, out string[] args)
        {
            var i = 0;
            var currentNode = _root;

            for (; i < parsedArgs.Length; i++)
            {
                if (!currentNode.Children.TryGetValue(parsedArgs[i].ToLowerInvariant(), out var newNode))
                    break;

                currentNode = newNode;
            }

            node = currentNode;
            args = parsedArgs[i..];
        }
    }
}

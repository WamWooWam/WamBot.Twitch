using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace WamBot.Twitch.Api
{
    public class CommandException : Exception
    {
        [JsonConstructor]
        private CommandException() { }

        public CommandException(string message) : base(message) { }
    }
}

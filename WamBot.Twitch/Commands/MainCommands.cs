using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.RateLimiter;
using TwitchLib.Api.Services;
using TwitchLib.Api.V5.Models.Users;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data;
using WamBot.Twitch.Services;

namespace WamBot.Twitch.Commands
{
    [Group("Main", "Main commands")]
    internal class MainCommands : CommandModule
    {
        private readonly TwitchAPI _twitchApi;
        private readonly BotDbContext _database;
        private readonly CommandRegistry _commandRegistry;
        private readonly LiveStreamMonitorService _liveStreamMonitor;
        private readonly IHttpClientFactory _factory;
        private readonly UserService _userService;

        private static RandomList<string> _pickupLines;
        private static Random _random = new Random();

        public MainCommands(
            IHttpClientFactory factory,
            CommandRegistry commandRegistry,
            BotDbContext database,
            TwitchAPI twitchAPI,
            UserService userService,
            LiveStreamMonitorService liveStreamMonitor)
        {
            _factory = factory;
            _twitchApi = twitchAPI;
            _database = database;
            _commandRegistry = commandRegistry;
            _liveStreamMonitor = liveStreamMonitor;
            _userService = userService;
        }

        [Command("Ping", "Pong!")]
        public void Ping(CommandContext ctx)
        {
            ctx.Reply("Pong!");
        }

        [RequireChannels("wambot_")]
        [Command("Add", "Adds WamBot to your chat!")]
        public async Task AddAsync(CommandContext ctx)
        {
            var dbChannel = await _database.DbChannels.FindAsync(long.Parse(ctx.Message.UserId));
            if (dbChannel != null)
            {
                ctx.Reply("I'm already in your chat! Use !remove to remove me.");
                return;
            }

            dbChannel = new DbChannel() { Id = long.Parse(ctx.Message.UserId), Name = ctx.Message.Username };
            _database.DbChannels.Add(dbChannel);
            await _database.SaveChangesAsync();

            _liveStreamMonitor.ChannelsToMonitor.Add(ctx.Message.Username);

            ctx.Client.JoinChannel(ctx.Message.Username);
            ctx.Reply("WamBot has been added to your chat!");
        }

        [RequireChannels("wambot_")]
        [Command("Remove", "Removes WamBot from your chat!")]
        public async Task RemoveAsync(CommandContext ctx)
        {
            var dbChannel = await _database.DbChannels.FindAsync(long.Parse(ctx.Message.UserId));
            if (dbChannel == null)
            {
                ctx.Reply("I'm not in your chat! Use !add to add me.");
                return;
            }

            _liveStreamMonitor.ChannelsToMonitor.Remove(ctx.Message.Username);
            _database.DbChannels.Remove(dbChannel);
            await _database.SaveChangesAsync();

            ctx.Client.LeaveChannel(ctx.Message.Username);
            ctx.Reply("WamBot has been removed from your chat!");
        }

        [Command("Help", "Help!")]
        public void Help(CommandContext ctx, params string[] args)
        {
            _commandRegistry.Lookup(args, out var node, out _);

            if (node.Parent == null) // slightly hacky root node 
            {
                var nodes = node.Children.Values;
                var commands = nodes.Where(c => c.Command != null && c.Command.CanExecute(ctx)).OrderBy(c => c.Command.Name).Select(c => $"'{c.Command.Aliases.First()}'").Distinct();
                var groups = nodes.Where(c => c.Command == null & c.Group != null).OrderBy(c => c.Group.Name).Select(c => $"'{c.Group.Aliases.First()}'").Distinct();

                ctx.Reply($"Currently available commands: {string.Join(", ", commands)}. Currently available groups: {string.Join(", ", groups)}.", true);
                return;
            }

            if (node.Command == null && node.Group != null)
            {
                var group = node.Group;
                var groupCommands = group.GetCommands();
                var commands = groupCommands.Where(c => c.CanExecute(ctx)).OrderBy(c => c.Name).Select(c => $"'{c.Aliases.First()}'").Distinct();

                ctx.Reply($"Currently available {node.Group.Name} commands: {string.Join(", ", commands)}.", true);
                return;
            }

            if (node.Command != null)
            {
                var command = node.Command;
                var commandUsage = command.Aliases[0];
                var thisNode = node;
                while (thisNode.Parent != null && thisNode.Parent.Group != null && thisNode.Parent.Group.Aliases.Length > 0)
                {
                    thisNode = thisNode.Parent;
                    commandUsage = thisNode.Group.Aliases[0] + " " + commandUsage;
                }

                ctx.Reply(
                    $"'{command.Name}': {(command.Description != null ? $"'{command.Description}'" : "")} " +
                    $"Aliases: {string.Join(", ", command.Aliases)}. " +
                    $"Usage: !{commandUsage} {command.Usage}", true);
            }
        }

        [Cooldown(30)]
        [Command("Privacy", "Information about how WamBot handles your data.")]
        public void Privacy(CommandContext ctx)
        {
            ctx.Reply(
                "WamBot does not log messages, users in chat, or commands run, but may keep command errors for debugging purposes. " +
                "For more information, see https://github.com/WamWooWam/WamBot.Twitch/blob/main/PRIVACY.md");
        }

        [OwnerOnly]
        [Command("Dump Info", "Dumps command info", "dump_")]
        public void Dump(CommandContext ctx, params string[] args)
        {
            _commandRegistry.Lookup(args, out var node, out _);
            if (node.Command != null)
                ctx.Reply($"@{ctx.Message.Username} {ReflectionUtilities.GetMethodDeclaration(node.Command.Method)}");
        }

        // @$(user) Check-in: +$(eval Math.floor(Math.random() * 10)) Onyx 💰 Play Raid: Shadow Legends ⚔️ ◉◉◉◉◎ https://bit.ly/3b9sOBX
        [Cooldown(3600, PerUser = true)]
        [Command("Onyx", "Yeah I totally have an onyx sponsorship honest...")]
        public async Task Onyx(CommandContext ctx)
        {
            var games = new string[] { "Raid: Shadow Legends", "Diner Dash", "Golf Clash", "Royal Casino", "Toon Blast", "Vikings: War of Clans", "Clash of Clans", "Fruit Ninja", "Angry Birds" };
            var links = new string[] { "https://bit.ly/3b9sOBX", "https://bit.ly/33JmJI3", "https://bit.ly/3bTpBFS", "https://bit.ly/3qhwo18", "https://bit.ly/3eOzHdZ", "https://bit.ly/3y7DWc9", "https://bit.ly/3eMW96Y" };

            var user = await _userService.GetOrCreateUserAsync(ctx.Message);
            var onyx = Math.Max(1, Math.Floor(Math.Abs(_random.RandomBiasedPow(0, 256, 128, 128) - 128)));
            user.OnyxPoints += (int)onyx;

            var x = Math.Min((int)((user.OnyxPoints / 500.0) * 5), 5);
            var y = 5 - x;

            var emoji = onyx != 1 ? "💰" : "😐";
            ctx.Reply($"@{ctx.Message.DisplayName} Check-in: +{(int)onyx} Onyx {emoji} Play {games[_random.Next(games.Length)]} ⚔️ {new string('◉', x)}{new string('◎', y)} {links[_random.Next(links.Length)]} ");
            await _database.SaveChangesAsync();
        }

        [Cooldown(30, PerUser = true)]
        [Command("Uwuify", "Make some text 100% furry approved", "uwu", "owo", "owoify", "uwuify")]
        public void Uwuify(CommandContext ctx, params string[] args)
        {
            ctx.Reply(string.Join(" ", args).Owofiy(_random));
        }

        [Cooldown(15, PerUser = true)]
        [Command("Pickup Line", "Select a random pickup line!", "pickup", "pickupline")]
        public void PickupLine(CommandContext ctx, IUser user = null)
        {
            if (_pickupLines == null)
                _pickupLines = new RandomList<string>(File.ReadAllLines(Path.Join(Directory.GetCurrentDirectory(), "pickup-lines.txt")));

            var line = _pickupLines.Next();
            var tripWords = new[] { "Hey", "Hi", "Babe", "Bitch", "Damn", "Girl", }; // words we remove from lines so targeted lines make sense
            var text = new StringBuilder();
            if (user != null)
            {
                text.Append($"Hey @{user.DisplayName} ");
                foreach (var word in tripWords)
                {
                    if (line.StartsWith(word, StringComparison.InvariantCultureIgnoreCase))
                        line = line.Substring(word.Length).TrimStart();
                }

                line = line.Trim().TrimStart(',');
                if (!line.StartsWith("I ") && !line.StartsWith("I'"))
                    line = char.ToLower(line[0]) + line.Substring(1);
            }

            text.Append(string.Format(line, ctx.Message.DisplayName));
            ctx.Reply(_random.NextDouble() > 0.9 ? text.ToString().Owofiy(_random) : text.ToString());
        }

        // https://www.google.com/maps?ll=-11.6626%2C-119.717&z=13
        // https://www.freemaptools.com/ajax/elevation-service.php?lat=21.36728&lng=9.77824
        [Cooldown(120)]
        [Command("Location", "Totally where I live wdym.", "location", "loc")]
        public async Task LocationAsync(CommandContext ctx, IUser user = null)
        {
            var client = _factory.CreateClient("OpenElevation");

            var latCoord = 0.0;
            var longCoord = 0.0;
            var elevation = 0.0f;

            do
            {
                latCoord = _random.Next(-86_000_000, 86_000_000) / 1_000_000.0;
                longCoord = _random.Next(-180_000_000, 180_000_000) / 1_000_000.0;

                var resp = await client.GetStringAsync("aster30m?locations=" + Uri.EscapeDataString(latCoord + "," + longCoord));
                var respJson = JObject.Parse(resp);
                var respElevation = respJson["results"][0]["elevation"];
                if (respElevation.Type != JTokenType.Null)
                    elevation = respElevation.ToObject<float>();

                await Task.Delay(2000);
            }
            while (elevation < 100);

            if (user != null)
            {
                ctx.Reply($"@{user.DisplayName}'s location is https://www.google.com/maps?q=" + latCoord + "%2C" + longCoord + "&z=6&t=k");
            }
            else
            {
                ctx.Reply("My location is https://www.google.com/maps?q=" + latCoord + "%2C" + longCoord + "&z=6&t=k");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Services;
using TwitchLib.Api;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data;
using TwitchLib.Api.V5.Models.Users;
using System.ComponentModel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WamBot.Twitch.Services;
using TwitchLib.Api.Core.Interfaces;

namespace WamBot.Twitch.Commands
{
    [Group("Cock", "The most accurate penis sizing algorithm you will ever see.", "pp", "penis")]
    public class PenisCommands : CommandModule
    {
        private readonly BotDbContext _database;
        private readonly ILogger<PenisCommands> _logger;
        private readonly UserService _userService;

        private static Random _random = new Random();
        private static readonly string[] _errorCodes = new[]
        {
            "400 Bad Request",
            "401 Unauthorised",
            "402 Payment Required",
            "403 Forbidden",
            "404 Not Found",
            "406 Not Acceptable",
            "410 Gone",
            "413 Payload Too Large",
            "418 I'm a teapot",
            "429 Too Many Requests",
            "450 Blocked by Windows Parental Controls",
            "500 Internal Server Error",
            "501 Not Implemented",
            "502 Bad Gateway",
            "503 Service Unavailable",
            "504 Gateway Timeout"
        };

        private static readonly string[] _penisEuphemisms = new[]
        {
            "cock", "dick", "penis", "schlong", "beef torpedo", "weiner",
            "shaft", "rod", "junk", "pecker", "prick", "wang",
        };

        private static readonly string[] _vaginaEuphemisms = new[]
        {
            "vagina", "pussy", "cunt", "twat",
        };

        public PenisCommands(
            BotDbContext database,
            UserService userService,
            ILogger<PenisCommands> logger)
        {
            _database = database;
            _logger = logger;
            _userService = userService;
        }

        [Default]
        [Command("Cock", "The most accurate penis sizing algorithm you will ever see.")]
        public async Task CockAsync(CommandContext ctx, string target = null)
        {
            var user = await (target == null ? _userService.GetOrCreateUserAsync(ctx.Message) : _userService.GetOrCreateUserAsync(target));

            await SendPenisMessageAsync(ctx, user, target);
        }

        [Command("Swordfight", "The biggest cock wins!", "fight", "duel")]
        public async Task SwordfightAsync(CommandContext ctx, string target)
        {
            var mainUser = await GetUserPenisInfoAsync(ctx, null);
            var targetUser = await GetUserPenisInfoAsync(ctx, target);
            var mainType = mainUser.dbUser.PenisType;
            var targetType = targetUser.dbUser.PenisType;

            if (mainType == PenisType.None)
            {
                ctx.Reply($"@{mainUser.userName} You have no penis.");
                return;
            }

            if (targetType == PenisType.None)
            {
                ctx.Reply($"@{targetUser.userName} has no penis.");
                return;
            }

            var mainSize = PenisUtils.CalculatePenisSize(mainUser.dbUser, out _);
            var targetSize = PenisUtils.CalculatePenisSize(targetUser.dbUser, out _);

            if ((targetType == PenisType.Inverse || mainType == PenisType.Inverse) && (targetType != PenisType.Inverse || mainType != PenisType.Inverse))
            {
                ctx.Reply($"The duel between @{mainUser.userName} and @{targetUser.userName} resulted in a tie! {FixCasing(_penisEuphemisms[_random.Next(_penisEuphemisms.Length)])} + {FixCasing(_vaginaEuphemisms[_random.Next(_vaginaEuphemisms.Length)])}");
                return;
            }

            var mainUserSize = (user: mainUser, size: mainSize, ratio: mainSize / Math.Max((double)(targetSize + mainSize), 1));
            var targetUserSize = (user: targetUser, size: targetSize, ratio: targetSize / Math.Max((double)(targetSize + mainSize), 1));

            var num = _random.NextDouble();
            var winner = num > mainUserSize.ratio ? targetUserSize : mainUserSize;
            var loser = num > mainUserSize.ratio ? mainUserSize : targetUserSize;
            ctx.Reply($"The winner is @{winner.user.userName}! {winner.size} inches vs {loser.size} inches.");
        }


        [Command("Set Cock", "Set your cock type.", "set")]
        public async Task SetCockAsync(CommandContext ctx)
        {
            var user = await _userService.GetOrCreateUserAsync(ctx.Message);
            if (user.PenisType == PenisType.Normal)
            {
                user.PenisType = PenisType.Inverse;
                ctx.Reply("You now have a vagina, congratulations!");
            }
            else if (user.PenisType == PenisType.Inverse)
            {
                user.PenisType = PenisType.None;
                ctx.Reply("You now have nothing, congratulations!");
            }
            else if (user.PenisType == PenisType.None)
            {
                user.PenisType = PenisType.Normal;
                ctx.Reply("You now have a penis, congratulations!");
            }

            await _userService.SaveChangesAsync();
        }

        [OwnerOnly]
        [Command("Admin Set Cock", null, "force")]
        public async Task SetCockAsync(CommandContext ctx, IUser targetUser, PenisType type)
        {
            var user = await _userService.GetOrCreateUserAsync(targetUser);
            user.PenisType = type;

            ctx.Reply($"Set {targetUser.Name}'s penis type to {type}");
            await _userService.SaveChangesAsync();
        }

        [Cooldown(30, PerUser = true)]
        [Command("Shuffle Cock", "Are you unhappy with your penis? For just W$100, shuffle it!", "shuffle")]
        public async Task ShuffleCockAsync(CommandContext ctx)
        {
            var channelUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, ctx.Message.Username);
            if (channelUser.Balance < 100.0m)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} You don't have enough money to shuffle your cock!");
                return;
            }

            channelUser.Balance -= 100;

            var dbUser = await _userService.GetOrCreateUserAsync(ctx.Message);
            dbUser.PenisOffset = _random.Next();

            await SendPenisMessageAsync(ctx, dbUser, ctx.Message.DisplayName);
            await _userService.SaveChangesAsync();
        }

        private async Task SendPenisMessageAsync(CommandContext ctx, DbUser dbUser, string name)
        {
            static string GetCockSizeEmote(double size)
            {
                if (size <= 2)
                    return "🤏 🔎 ";
                if (size <= 4)
                    return "source6Approve ";

                return "";
            }

            if (dbUser == null)
            {
                ctx.Reply($"Failed to calculate @{name}'s penis size: {_errorCodes[_random.Next(_errorCodes.Length)]}.");
                return;
            }

            var twitchUser = await _userService.GetTwitchUserAsync(dbUser);
            if (twitchUser == null || dbUser.PenisType == PenisType.None)
            {
                ctx.Reply($"Failed to calculate @{twitchUser?.DisplayName ?? name}'s penis size: {_errorCodes[_random.Next(_errorCodes.Length)]}.");
                return;
            }

            var random = new Random((int)(dbUser.Id + dbUser.PenisOffset));
            double size;
            switch (dbUser.PenisType)
            {
                case PenisType.Tiny:
                case PenisType.Normal:
                case PenisType.Large:
                    size = PenisUtils.CalculatePenisSize(dbUser, out var fmt);
                    ctx.Reply($"{GetCockSizeEmote(size)}@{twitchUser.DisplayName}'s {_penisEuphemisms[_random.Next(_penisEuphemisms.Length)]} is {size.ToString(fmt)} {(size == 1 ? "inch" : "inches")} ({(size * 2.5).ToString(fmt)}cm) long! 8{new string('=', (int)size)}D");
                    return;
                case PenisType.Inverse:
                    size = (int)Math.Floor(random.RandomNormal(6, 24, 4));
                    ctx.Reply($"@{twitchUser.DisplayName}'s {_vaginaEuphemisms[_random.Next(_vaginaEuphemisms.Length)]} is {size:N0} {(size == 1 ? "inch" : "inches")} ({size * 2.5:N0}cm) deep! ){new string('=', (int)size)}8");
                    return;
            }
        }

        private string FixCasing(string s)
        {
            if (char.IsLower(s[0]))
                return char.ToUpper(s[0]) + s[1..];

            return s;
        }

        private async Task<(long userId, DbUser dbUser, string displayName, string userName)> GetUserPenisInfoAsync(CommandContext ctx, string target)
        {
            var userId = 0L;
            var displayName = target;
            var userName = target;

            if (string.IsNullOrWhiteSpace(target))
            {
                userId = long.Parse(ctx.Message.UserId);
                displayName = ctx.Message.DisplayName;
                userName = ctx.Message.Username;
            }
            else
            {
                var user = await _userService.GetTwitchUserAsync(target);
                if (user != null)
                {
                    userId = long.Parse(user.Id);
                    displayName = user.DisplayName;
                    userName = user.Name;
                }
                else
                {
                    userId = target.GetHashCode();
                }
            }

            var dbUser = await _userService.GetOrCreateUserAsync(target);
            return (userId, dbUser, displayName, userName);
        }

    }
}

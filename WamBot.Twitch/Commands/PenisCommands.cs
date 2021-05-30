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
using WamBot.Twitch.Services;
using TwitchLib.Api.V5.Models.Users;

namespace WamBot.Twitch.Commands
{
    [Group("Cock", "The most accurate penis sizing algorithm you will ever see.", "pp", "penis")]
    public class PenisCommands : CommandModule
    {
        private readonly TwitchAPI _twitchApi;
        private readonly BotDbContext _database;
        private readonly IParamConverter<User> _userConverter;

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
            IParamConverter<User> userConverter,
            BotDbContext database,
            TwitchAPI twitchAPI)
        {
            _twitchApi = twitchAPI;
            _database = database;
            _userConverter = userConverter;
        }

        [Default]
        [Command("Cock", "The most accurate penis sizing algorithm you will ever see.")]
        public async Task CockAsync(CommandContext ctx, string target = null)
        {
            var info = await GetUserPenisInfoAsync(ctx, target);

            static string GetCockSizeEmote(double size)
            {
                if (size <= 2)
                    return "🤏 🔎 ";
                if (size <= 4)
                    return "source6Approve ";

                return "";
            }

            var random = new Random((int)info.userId + info.dbUser.PenisOffset);
            double size;
            switch (info.dbUser?.PenisType ?? PenisType.None)
            {
                case PenisType.None:
                    ctx.Reply($"Failed to calculate @{info.userName}'s penis size: {_errorCodes[_random.Next(_errorCodes.Length)]}.");
                    return;
                case PenisType.Tiny:
                case PenisType.Normal:
                case PenisType.Large:
                    size = CalculatePenisSize(info.dbUser, info.userId, out var fmt);
                    ctx.Reply($"{GetCockSizeEmote(size)}@{info.userName}'s {_penisEuphemisms[_random.Next(_penisEuphemisms.Length)]} is {size.ToString(fmt)} {(size == 1 ? "inch" : "inches")} ({(size * 2.5).ToString(fmt)}cm) long! 8{new string('=', (int)size)}D");
                    return;
                case PenisType.Inverse:
                    size = (int)Math.Floor(random.RandomNormal(6, 24, 4));
                    ctx.Reply($"@{info.userName}'s {_vaginaEuphemisms[_random.Next(_vaginaEuphemisms.Length)]} is {size:N0} {(size == 1 ? "inch" : "inches")} ({size * 2.5:N0}cm) deep! ){new string('=', (int)size)}8");
                    return;
            }
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

            var mainSize = CalculatePenisSize(mainUser.dbUser, mainUser.userId, out _);
            var targetSize = CalculatePenisSize(targetUser.dbUser, mainUser.userId, out _);

            if (mainSize == targetSize)
            {
                ctx.Reply($"The duel between @{mainUser.userName} and @{targetUser.userName} resulted in a tie! {mainSize} inches vs. {targetSize} inches");
                return;
            }

            if ((targetType == PenisType.Inverse || mainType == PenisType.Inverse) && (targetType != PenisType.Inverse || mainType != PenisType.Inverse))
            {
                ctx.Reply($"The duel between @{mainUser.userName} and @{targetUser.userName} resulted in a tie! {FixCasing(_penisEuphemisms[_random.Next(_penisEuphemisms.Length)])} + {FixCasing(_vaginaEuphemisms[_random.Next(_vaginaEuphemisms.Length)])}");
                return;
            }

            var winner = mainSize > targetSize ? mainUser : targetUser;
            ctx.Reply($"The winner is @{winner.userName}! {Math.Max(mainSize, targetSize)} inches vs {Math.Min(mainSize, targetSize)} inches.");
        }


        [Command("Set Cock", "Set your cock type.", "set")]
        public async Task SetCockAsync(CommandContext ctx)
        {
            var id = long.Parse(ctx.Message.UserId);
            var dbUser = await _database.DbUsers.FindAsync(ctx.Message.Username);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = ctx.Message.Username };
                _database.DbUsers.Add(dbUser);
            }

            if (dbUser.PenisType == PenisType.Normal)
            {
                dbUser.PenisType = PenisType.Inverse;
                ctx.Reply("You now have a vagina, congratulations!");
            }
            else if (dbUser.PenisType == PenisType.Inverse)
            {
                dbUser.PenisType = PenisType.None;
                ctx.Reply("You now have nothing, congratulations!");
            }
            else if (dbUser.PenisType == PenisType.None)
            {
                dbUser.PenisType = PenisType.Normal;
                ctx.Reply("You now have a penis, congratulations!");
            }

            await _database.SaveChangesAsync();
        }

        [OwnerOnly]
        [Command("Admin Set Cock", null, "force")]
        public async Task SetCockAsync(CommandContext ctx, User targetUser, PenisType type)
        {
            var id = long.Parse(targetUser.Id);
            var dbUser = await _database.DbUsers.FindAsync(targetUser.Name);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = targetUser.Name };
                _database.DbUsers.Add(dbUser);
            }

            ctx.Reply($"Set {targetUser.Name}'s penis type to {type}");
            dbUser.PenisType = type;
            await _database.SaveChangesAsync();
        }

        [Command("Shuffle Cock", "Are you unhappy with your penis? For just W$100, shuffle it!", "shuffle")]
        public async Task ShuffleCockAsync(CommandContext ctx)
        {
            var channelUser = EconomyUtils.GetOrCreateChannelUser(_database, _twitchApi, ctx.Message.Channel, ctx.Message.Username);
            if (channelUser.Balance < 100.0m)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} You don't have enough money to shuffle your cock!");
                return;
            }

            channelUser.Balance -= 100;

            var dbUser = await _database.DbUsers.FindAsync(ctx.Message.Username);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = ctx.Message.Username };
                _database.DbUsers.Add(dbUser);
            }

            dbUser.PenisOffset = _random.Next();
            ctx.Reply($"@{ctx.Message.DisplayName} Your {(dbUser.PenisType == PenisType.Inverse ? "vagina" : "penis")} size has been shuffled!");

            await _database.SaveChangesAsync();
        }


        public string FixCasing(string s)
        {
            if (char.IsLower(s[0]))
                return char.ToUpper(s[0]) + s.Substring(1);

            return s;
        }

        public double CalculatePenisSize(DbUser user, long userId, out string formatString)
        {
            formatString = "N0";
            var random = new Random((int)userId + user.PenisOffset);
            switch (user.PenisType)
            {
                case PenisType.Tiny:
                    formatString = "N2";
                    return random.RandomNormal(0, 4, 4);
                case PenisType.Normal:
                    return (int)Math.Floor(random.RandomBiasedPow(0, 24, 4, 6));
                case PenisType.Large:
                    return (int)Math.Floor(random.RandomBiasedPow(0, 24, 2, 12));
                case PenisType.Inverse:
                    return (int)Math.Floor(random.RandomNormal(6, 24, 4));
                default:
                case PenisType.None:
                    throw new InvalidOperationException("Can't calculate length of non-existant penis.");
            }
        }

        public async Task<(long userId, DbUser dbUser, string displayName, string userName)> GetUserPenisInfoAsync(CommandContext ctx, string target)
        {
            var userId = 0L;
            var displayName = target;
            var userName = target;
            var isUser = true;

            if (string.IsNullOrWhiteSpace(target))
            {
                userId = long.Parse(ctx.Message.UserId);
                displayName = ctx.Message.DisplayName;
                userName = ctx.Message.Username;
            }
            else
            {
                var user = (User)await _userConverter.Convert(target, ctx);
                if (user != null)
                {
                    userId = long.Parse(user.Id);
                    displayName = user.DisplayName;
                    userName = user.Name;
                }
                else
                {
                    userId = target.GetHashCode();
                    displayName = target;
                    isUser = false;
                }
            }

            var dbUser = await _database.DbUsers.FindAsync(userName);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = userName, PenisType = isUser ? PenisType.Normal : PenisType.None };
                if (isUser)
                    _database.DbUsers.Add(dbUser);
            }

            return (userId, dbUser, displayName, userName);
        }

    }
}

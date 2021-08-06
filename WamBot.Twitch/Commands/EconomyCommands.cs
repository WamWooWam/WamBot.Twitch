using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.Services;
using TwitchLib.Api;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data;
using TwitchLib.Api.V5.Models.Users;
using WamBot.Twitch.Services;
using TwitchLib.Api.Core.Interfaces;

namespace WamBot.Twitch.Commands
{
    public class EconomyCommands : CommandModule
    {
        private static readonly Random _random = new Random();
        private readonly ILogger<EconomyCommands> _logger;
        private readonly BotDbContext _dbContext;
        private readonly TwitchAPI _twitchAPI;
        private readonly UserService _userService;

        public EconomyCommands(
            ILogger<EconomyCommands> logger,
            BotDbContext dbContext,
            TwitchAPI twitchAPI,
            UserService userServce)
        {
            _logger = logger;
            _dbContext = dbContext;
            _twitchAPI = twitchAPI;
            _userService = userServce;
        }

        [Command("Balance", "How much cash do you have?", "bal")]
        public async Task Balance(CommandContext ctx)
        {
            var channelUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, ctx.Message.Username);
            ctx.Reply($"@{ctx.Message.DisplayName} you have W${channelUser.Balance:N2}");
        }

        [BlockChannels("imjhay_", WithPrefixes = new[] { "!" })]
        [Command("Duel", "Pit your WamCoin against another person in chat.", "duel")]
        public async Task DuelAsync(CommandContext ctx, IUser user, decimal amount = 0)
        {
            var currentUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, ctx.Message.Username);
            var otherUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, user.Name);

            if (!EnsureBalance(ctx, currentUser, amount))
                return;

            if (otherUser.Balance < amount)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} @{user.DisplayName} only has {otherUser.Balance.FormatCash()}!");
                return;
            }

            ctx.Reply($"@{user.DisplayName}, {ctx.Message.DisplayName} wants to duel you for {amount.FormatCash()}! Type !accept to accept, and !deny to deny.");
            if (!await ctx.Interactivity.WaitForYesNoAsync(user.Name))
                return;

            var total = amount * 2;
            currentUser.Balance -= amount;
            otherUser.Balance -= amount;

            var num = _random.NextDouble();
            var winner = num > 0.5 ? currentUser : otherUser;
            var loser = num > 0.5 ? otherUser : currentUser;
            winner.Balance += total;

            winner.DbUser.IncrementConsecutiveWins();
            loser.DbUser.IncrementConsecutiveLosses();

            var winnerTwitchUser = await _userService.GetTwitchUserAsync(winner.UserId);
            ctx.Reply($"@{winnerTwitchUser} has won the duel and now has {winner.Balance.FormatCash()} with {winner.DbUser.ConsecutiveWins} consecutive wins!");
            await _dbContext.SaveChangesAsync();
        }

        [OwnerOnly]
        [Command("Cheat", "How much cash do you have?", "give", "cheat")]
        public async Task CheatAsync(CommandContext ctx, decimal give, IUser user = null)
        {
            var channelUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, user?.Name ?? ctx.Message.Username);
            channelUser.Balance += give;

            ctx.Reply($"@{user?.Name ?? ctx.Message.Username} you now have W${channelUser.Balance:N2}");
            await _dbContext.SaveChangesAsync();
        }

        [BlockChannels("imjhay_", WithPrefixes = new[] { "!" })]
        [Command("Gamble", "Throw away your money because you're stupid :)")]
        public async Task GambleAsync(CommandContext ctx, string rawAmount)
        {
            var channelUser = await _userService.GetOrCreateChannelUserAsync(ctx.Message.Channel, ctx.Message.Username);
            if (channelUser.Balance == 0)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} you have no WamBux!");
                return;
            }

            var amount = ParseAmount(rawAmount, channelUser);
            if (amount == null)
                return;

            if ((channelUser.Balance - amount) < 0)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} you only have {channelUser.Balance.FormatCash()}!");
                return;
            }

            channelUser.Balance -= amount.Value;
            if (_random.NextDouble() > 0.45)
            {
                channelUser.Balance += amount.Value * 2;
                ctx.Reply($"@{ctx.Message.DisplayName} just won {(amount.Value * 2).FormatCash()} and now has {channelUser.Balance.FormatCash()}!");
                channelUser.DbUser.IncrementConsecutiveWins();
            }
            else
            {
                ctx.Reply($"@{ctx.Message.DisplayName} just lost {amount.Value.FormatCash()} and now has {channelUser.Balance.FormatCash()}!");
                channelUser.DbUser.IncrementConsecutiveLosses();
            }

            await _dbContext.SaveChangesAsync();
        }

        private bool EnsureBalance(CommandContext ctx, DbChannelUser user, decimal amount)
        {
            if (user.Balance < amount)
            {
                ctx.Reply($"@{ctx.Message.DisplayName} you'll need {amount.FormatCash()} to do that!");
                return false;
            }

            return true;
        }

        private decimal? ParseAmount(string rawAmount, DbChannelUser channelUser)
        {
            rawAmount = rawAmount.ToLowerInvariant().Trim();
            decimal amount;
            if (rawAmount == "all")
                amount = channelUser.Balance;
            else if (rawAmount == "half")
                amount = channelUser.Balance / 2;
            else if (!decimal.TryParse(rawAmount, out amount))
                return null;

            return Math.Round(amount, 2);
        }
    }
}

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

namespace WamBot.Twitch.Commands
{
    public class EconomyCommands : CommandModule
    {
        private static readonly Random _random = new Random();

        private readonly ILogger<EconomyCommands> _logger;
        private readonly BotDbContext _dbContext;
        private readonly TwitchAPI _twitchAPI;

        public EconomyCommands(
            ILogger<EconomyCommands> logger,
            BotDbContext dbContext,
            TwitchAPI twitchAPI)
        {
            _logger = logger;
            _dbContext = dbContext;
            _twitchAPI = twitchAPI;
        }

        [Command("Balance", "How much cash do you have?", "bal")]
        public void Balance(CommandContext ctx)
        {
            var channelUser = EconomyUtils.GetOrCreateChannelUser(_dbContext, _twitchAPI, ctx.Message.Channel, ctx.Message.Username);
            ctx.Reply($"@{ctx.Message.DisplayName} you have W${channelUser.Balance:N2}");
        }

        [OwnerOnly]
        [Command("Cheat", "How much cash do you have?", "give", "cheat")]
        public async Task CheatAsync(CommandContext ctx, decimal give, User user = null)
        {
            var channelUser = EconomyUtils.GetOrCreateChannelUser(_dbContext, _twitchAPI, ctx.Message.Channel, user?.Name ?? ctx.Message.Username);
            channelUser.Balance += give;

            ctx.Reply($"@{user?.Name ?? ctx.Message.Username} you now have W${channelUser.Balance:N2}");
            await _dbContext.SaveChangesAsync();
        }

        [BlockChannels("imjhay_")]
        [Command("Gamble", "Throw away your money because you're stupid :)")]
        public async Task GambleAsync(CommandContext ctx, string rawAmount)
        {
            var channelUser = EconomyUtils.GetOrCreateChannelUser(_dbContext, _twitchAPI, ctx.Message.Channel, ctx.Message.Username);
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
                ctx.Reply($"@{ctx.Message.DisplayName} you only have {EconomyUtils.FormatCash(channelUser.Balance)}!");
                return;
            }

            channelUser.Balance -= amount.Value;
            if (_random.NextDouble() > 0.45)
            {
                channelUser.Balance += amount.Value * 2;
                ctx.Reply($"@{ctx.Message.DisplayName} just won {EconomyUtils.FormatCash(amount.Value * 2)} and now has {EconomyUtils.FormatCash(channelUser.Balance)}!");
            }
            else
            {
                ctx.Reply($"@{ctx.Message.DisplayName} just lost {EconomyUtils.FormatCash(amount.Value)} and now has {EconomyUtils.FormatCash(channelUser.Balance)}!");
            }

            await _dbContext.SaveChangesAsync();
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

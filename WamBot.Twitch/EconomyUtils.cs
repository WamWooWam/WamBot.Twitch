using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using WamBot.Twitch.Data;

namespace WamBot.Twitch
{
    class EconomyUtils
    {
        public static string FormatCash(decimal cash)
        {
            return $"W${cash:N2}";
        }

        public static DbChannelUser GetOrCreateChannelUser(BotDbContext database, TwitchAPI twitchAPI, string channel, string username)
        {
            var user = database.DbChannelUsers.Find(username, channel);
            if (user != null)
                return user;

            var dbUser = database.DbUsers.Find(username);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = username };
                database.DbUsers.Add(dbUser);
            }

            var dbChannel = database.DbChannels.Find(channel);
            if (dbChannel == null)
            {
                dbChannel = new DbChannel() { Name = channel };
                database.DbChannels.Add(dbChannel);
            }

            user = new DbChannelUser() { UserName = username, ChannelName = channel, Balance = 0, DbUser = dbUser, DbChannel = dbChannel };
            database.DbChannelUsers.Add(user);

            return user;
        }
    }
}

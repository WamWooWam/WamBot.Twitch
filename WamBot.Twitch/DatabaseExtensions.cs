using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client.Models;
using WamBot.Twitch.Data;
using HelixUser = TwitchLib.Api.Helix.Models.Users.GetUsers.User;
using V5User = TwitchLib.Api.V5.Models.Users.User;

namespace WamBot.Twitch
{
    public static class DatabaseExtensions
    {
        public static async Task<DbChannelUser> GetOrCreateChannelUserAsync(this BotDbContext database, TwitchAPI api, string channel, string username, bool ensurePopulated = true)
        {
            if (!ensurePopulated)
                return await database.DbChannelUsers.FirstOrDefaultAsync(u => u.ChannelName == channel && u.UserName == username);

            var user = await database.DbChannelUsers.Include(u => u.DbUser)
                                                    .Include(u => u.DbChannel)
                                                    .FirstOrDefaultAsync(u => u.ChannelName == channel && u.UserName == username);

            if (user != null)
                return user;

            var twitchUsers = await api.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
            var twitchUser = twitchUsers.Users.FirstOrDefault();
            if (twitchUser == null)
                return null;

            var dbUser = await GetOrCreateUserAsync(database, twitchUser);
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

        public static async Task<DbUser> GetOrCreateUserAsync(this BotDbContext database, ChatMessage message)
        {
            var dbUser = await database.DbUsers.FirstOrDefaultAsync(u => u.Name == message.Username);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = message.Username, Id = long.Parse(message.UserId) };
                database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }

        public static async Task<DbUser> GetOrCreateUserAsync(this BotDbContext database, HelixUser user)
        {
            var dbUser = await database.DbUsers.FirstOrDefaultAsync(u => u.Name == user.Login);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = user.Login, Id = long.Parse(user.Id) };
                database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }

        public static async Task<DbUser> GetOrCreateUserAsync(this BotDbContext database, V5User user)
        {
            var dbUser = await database.DbUsers.FirstOrDefaultAsync(u => u.Name == user.Name);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Name = user.Name, Id = long.Parse(user.Id) };
                database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }

        public static async Task<DbUser> GetOrCreateUserAsync(this BotDbContext database, TwitchAPI api, string username)
        {
            var dbUser = await database.DbUsers.FirstOrDefaultAsync(u => u.Name == username);
            if (dbUser == null)
            {
                var twitchUsers = await api.Helix.Users.GetUsersAsync(logins: new List<string>() { username });
                var twitchUser = twitchUsers.Users.FirstOrDefault();
                if (twitchUser == null)
                    return null;

                dbUser = new DbUser() { Name = twitchUser.Login, Id = long.Parse(twitchUser.Id) };
                database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }
    }
}

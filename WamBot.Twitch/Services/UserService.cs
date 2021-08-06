using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.V5.Models.Users;
using WamBot.Twitch.Data;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Api;
using TwitchLib.Client.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace WamBot.Twitch.Services
{
    public class UserService
    {
        private readonly ITwitchAPI _twitchApi;
        private readonly IMemoryCache _userCache;
        private readonly ILogger<UserService> _logger;
        private readonly BotDbContext _database;

        public UserService(
            TwitchAPI twitchApi,
            IMemoryCache memoryCache,
            ILogger<UserService> logger,
            BotDbContext database)
        {
            _twitchApi = twitchApi;
            _userCache = memoryCache;
            _logger = logger;
            _database = database;
        }

        public Task<IUser> GetTwitchUserAsync(DbUser dbUser)
            => GetTwitchUserAsync(dbUser.Id);

        public async Task<IUser> GetTwitchUserAsync(string userName)
        {
            userName = userName.ToLowerInvariant().TrimStart('@');
            try
            {
                if (_userCache.TryGetValue($"UserName_{userName}", out IUser user))
                {
                    return user;
                }

                var users = await _twitchApi.V5.Users.GetUserByNameAsync(userName);
                user = users.Matches.FirstOrDefault(u => u.Name == userName);
                if (user == null)
                    return user;

                _userCache.Set($"User_{user.Id}", user, DateTimeOffset.Now + TimeSpan.FromMinutes(10));
                return _userCache.Set($"UserName_{userName}", user, DateTimeOffset.Now + TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Twitch User for {Id}", userName);
            }

            return null;
        }

        public async Task<IUser> GetTwitchUserAsync(long id)
        {
            try
            {
                if (_userCache.TryGetValue($"User_{id}", out IUser user))
                {
                    return user;
                }

                user = await _twitchApi.V5.Users.GetUserByIDAsync(id.ToString());
                _userCache.Set($"UserName_{user.Name.ToLowerInvariant()}", user, DateTimeOffset.Now + TimeSpan.FromMinutes(10));
                return _userCache.Set($"User_{id}", user, DateTimeOffset.Now + TimeSpan.FromMinutes(10));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get Twitch User for {Id}", id);
            }

            return null;
        }


        public async Task<DbChannelUser> GetOrCreateChannelUserAsync(string channel, string username, bool ensurePopulated = true)
        {
            var channelUser = await GetTwitchUserAsync(channel);
            var usernameUser = await GetTwitchUserAsync(username);

            if (channelUser == null || usernameUser == null)
                return null;

            var channelId = long.Parse(channelUser.Id);
            var userId = long.Parse(usernameUser.Id);

            if (!ensurePopulated)
                return await _database.DbChannelUsers.FirstOrDefaultAsync(u => u.ChannelId == channelId && u.UserId == userId);

            var user = await _database.DbChannelUsers.Include(u => u.DbUser)
                                                     .Include(u => u.DbChannel)
                                                     .FirstOrDefaultAsync(u => u.ChannelId == channelId && u.UserId == userId);

            if (user != null)
                return user;

            var dbUser = await this.GetOrCreateUserAsync(usernameUser);
            var dbChannel = _database.DbChannels.Find(channelId);
            if (dbChannel == null)
            {
                dbChannel = new DbChannel() { Id = channelId, Name = channelUser.Name };
                _database.DbChannels.Add(dbChannel);
            }

            user = new DbChannelUser() { UserId = dbUser.Id, ChannelId = dbChannel.Id, Balance = 0, DbUser = dbUser, DbChannel = dbChannel };
            _database.DbChannelUsers.Add(user);

            return user;
        }

        public async Task<DbUser> GetOrCreateUserAsync(ChatMessage message)
        {
            var dbUser = await _database.DbUsers.FirstOrDefaultAsync(u => u.Id == long.Parse(message.UserId));
            if (dbUser == null)
            {
                dbUser = new DbUser() { Id = long.Parse(message.UserId), Name = message.Username };
                _database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }

        public async Task<DbUser> GetOrCreateUserAsync(IUser user)
        {
            var dbUser = await _database.DbUsers.FirstOrDefaultAsync(u => u.Id == long.Parse(user.Id));
            if (dbUser == null)
            {
                dbUser = new DbUser() { Id = long.Parse(user.Id), Name = user.Name };
                _database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }

        public async Task<DbUser> GetOrCreateUserAsync(string username, IUser twitchUser = null)
        {
            if (twitchUser == null)
                twitchUser = await GetTwitchUserAsync(username);

            var twitchUserId = long.Parse(twitchUser.Id);
            var dbUser = await _database.DbUsers.FirstOrDefaultAsync(u => u.Id == twitchUserId);
            if (dbUser == null)
            {
                dbUser = new DbUser() { Id = twitchUserId, Name = twitchUser.Name };
                _database.DbUsers.Add(dbUser);
            }

            return dbUser;
        }
    }
}

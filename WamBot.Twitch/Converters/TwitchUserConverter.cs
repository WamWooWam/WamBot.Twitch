using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;
using WamBot.Twitch.Api;

namespace WamBot.Twitch.Converters
{
    public class TwitchUserConverter : IParamConverter<User>
    {
        static Regex UserRegex = new Regex("^([#@])?[a-zA-Z0-9][\\w]{2,24}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        private readonly TwitchAPI _api;
        public TwitchUserConverter(TwitchAPI api)
        {
            _api = api;
        }

        public async Task<object> Convert(string arg, CommandContext context)
        {
            arg = arg.TrimStart('@');
            if (!string.IsNullOrWhiteSpace(arg) && UserRegex.IsMatch(arg))
            {
                var users = await _api.V5.Users.GetUserByNameAsync(arg);
                if (users.Matches.Length > 0)
                {
                    return users.Matches[0];
                }
            }

            return null;
        }
    }
}

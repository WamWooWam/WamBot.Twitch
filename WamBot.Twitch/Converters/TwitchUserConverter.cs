using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.V5.Models.Users;
using WamBot.Twitch.Api;
using WamBot.Twitch.Services;

namespace WamBot.Twitch.Converters
{
    public class TwitchUserConverter : IParamConverter<IUser>
    {
        static Regex UserRegex = new Regex("^([#@])?[a-zA-Z0-9][\\w]{2,24}$", RegexOptions.Compiled | RegexOptions.ECMAScript);

        private readonly UserService _userService;
        public TwitchUserConverter(UserService api)
        {
            _userService = api;
        }

        public async Task<object> Convert(string arg, CommandContext context)
        {
            if (!string.IsNullOrWhiteSpace(arg) && UserRegex.IsMatch(arg))
            {
                arg = arg.TrimStart('@');
                return await _userService.GetTwitchUserAsync(arg);
            }

            return null;
        }
    }
}

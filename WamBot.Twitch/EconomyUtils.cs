using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using WamBot.Twitch.Data;

namespace WamBot.Twitch
{
    public static class EconomyUtils
    {
        public static string FormatCash(this decimal cash)
        {
            return $"W${cash:N2}";
        }
    }
}

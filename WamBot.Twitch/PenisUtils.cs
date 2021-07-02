using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WamBot.Twitch.Data;

namespace WamBot.Twitch
{
    public class PenisUtils
    {
        public static double CalculatePenisSize(DbUser user, out string formatString)
        {
            formatString = "N0";
            var random = new Random((int)user.Id + user.PenisOffset);
            switch (user.PenisType)
            {
                case PenisType.Tiny:
                    formatString = "N2";
                    return random.RandomNormal(0, 4, 4);
                case PenisType.Normal:
                    return Math.Floor(random.RandomBiasedPow(0, 24, 4, 6));
                case PenisType.Large:
                    return Math.Floor(random.RandomBiasedPow(0, 24, 2, 12));
                case PenisType.Inverse:
                    return Math.Floor(random.RandomNormal(6, 24, 4));
                default:
                case PenisType.None:
                    return 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WamBot.Twitch.Api;

namespace WamBot.Twitch
{
    public static class Extensions
    {
        public static double TimestampToMilliseconds(long timestamp)
        {
            return (double)(Stopwatch.GetTimestamp() - timestamp) / (Stopwatch.Frequency / 1000);
        }

        internal static char[] _quotes = new[] { '"', '”', '“', '\'', '`' };

        public static IServiceCollection AddParamConverter<T, TImpl>(this IServiceCollection services) where TImpl : IParamConverter<T>
        {
            return services.AddTransient<IParamConverter<T>>((services) => ActivatorUtilities.CreateInstance<TImpl>(services));
        }

        public static IHost Build<TStartup>(this IHostBuilder builder) where TStartup : class, IStartup
        {
            builder.ConfigureServices((c, s) =>
            {
                var startup = ActivatorUtilities.GetServiceOrCreateInstance<TStartup>(s.BuildServiceProvider());
                startup.Configure(builder);
                startup.ConfigureServices(s);
            });

            return builder.Build();
        }

        internal static bool IsParams(this ParameterInfo param)
        {
            return param.IsDefined(typeof(ParamArrayAttribute), false);
        }

        public static void Shuffle<T>(this IList<T> list, Random rand)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rand.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string Owofiy(this string str, Random rand)
        {
            var faces = new[] { "(・`ω´・)", ";;w;;", "owo", "UwU", ">w<", "^w^", "😳", "🥺" };
            str = Regex.Replace(str, "(?:r|l)", "w", RegexOptions.ECMAScript);
            str = Regex.Replace(str, "(?:R|L)", "W", RegexOptions.ECMAScript);
            str = Regex.Replace(str, "n([aeiou])", (m) => $"ny{m.Groups[1].Value}", RegexOptions.ECMAScript);
            str = Regex.Replace(str, "N([aeiou])", (m) => $"Ny{m.Groups[1].Value}", RegexOptions.ECMAScript);
            str = Regex.Replace(str, "N([AEIOU])", (m) => $"Ny{m.Groups[1].Value}", RegexOptions.ECMAScript);
            str = Regex.Replace(str, "ove", "uv", RegexOptions.ECMAScript);

            str += " " + faces[rand.Next(faces.Length)];

            return str;
        }

        internal static IEnumerable<Command> GetCommands(Type t)
        {
            var methods = t
                .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                .Where(m => m.CustomAttributes.Any(a => a.AttributeType == typeof(CommandAttribute)))
                .Where(m => m.ReturnType == typeof(Task) || m.ReturnType == typeof(void))
                .Select(m => new Command(m, t));

            return methods;
        }

        public static IEnumerable<string> SplitCommandLine(this string commandLine)
        {
            var inQuotes = false;

            return commandLine.Split((b, c, a) =>
            {
                if (_quotes.Contains(c) && b != '\\')
                {
                    inQuotes = !inQuotes;
                }

                return !inQuotes && c == ' ';
            }).Select(arg => arg.Trim().TrimMatchingQuotes());
        }

        public static IEnumerable<string> Split(this string str, Func<char, char, char, bool> controller)
        {
            var nextPiece = 0;

            for (var c = 0; c < str.Length; c++)
            {
                if (controller(c > 0 ? str[c - 1] : default(char), str[c], c < str.Length - 1 ? str[c + 1] : default(char)))
                {
                    yield return str.Substring(nextPiece, c - nextPiece);
                    nextPiece = c + 1;
                }
            }

            yield return str.Substring(nextPiece);
        }

        public static IEnumerable<List<T>> Split<T>(this List<T> list, int size = 30)
        {
            for (int i = 0; i < list.Count; i += size)
            {
                yield return list.GetRange(i, Math.Min(size, list.Count - i));
            }
        }

        public static string TrimMatchingQuotes(this string input)
        {
            if ((input.Length >= 2) && (_quotes.Contains(input[0])) && (_quotes.Contains(input[input.Length - 1])))
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }

        public static double RandomNormal(this Random r, double min, double max, int tightness)
        {
            double total = 0.0;
            for (int i = 1; i <= tightness; i++)
            {
                total += r.NextDouble();
            }
            return ((total / tightness) * (max - min)) + min;
        }

        public static double RandomNormalDist(this Random r, double min, double max, int tightness, double exp)
        {
            double total = 0.0;
            for (int i = 1; i <= tightness; i++)
            {
                total += Math.Pow(r.NextDouble(), exp);
            }

            return ((total / tightness) * (max - min)) + min;
        }


        public static double RandomBiasedPow(this Random r, double min, double max, int tightness, double peak)
        {
            // Calculate skewed normal distribution, skewed by Math.Pow(...), specifiying where in the range the peak is
            // NOTE: This peak will yield unreliable results in the top 20% and bottom 20% of the range.
            //       To peak at extreme ends of the range, consider using a different bias function

            double total = 0.0;
            double scaledPeak = peak / (max - min) + min;

            if (scaledPeak < 0.2 || scaledPeak > 0.8)
            {
                throw new Exception("Peak cannot be in bottom 20% or top 20% of range.");
            }

            double exp = GetExp(scaledPeak);

            for (int i = 1; i <= tightness; i++)
            {
                // Bias the random number to one side or another, but keep in the range of 0 - 1
                // The exp parameter controls how far to bias the peak from normal distribution
                total += Math.Pow(r.NextDouble(), exp);
            }

            return ((total / tightness) * (max - min)) + min;
        }

        public static double GetExp(double peak)
        {
            // Get the exponent necessary for BiasPow(...) to result in the desired peak 
            // Based on empirical trials, and curve fit to a cubic equation, using WolframAlpha
            return -12.7588 * Math.Pow(peak, 3) + 27.3205 * Math.Pow(peak, 2) - 21.2365 * peak + 6.31735;
        }
    }

    public interface IStartup
    {
        void ConfigureServices(IServiceCollection services);

        void Configure(IHostBuilder host);
    }
}

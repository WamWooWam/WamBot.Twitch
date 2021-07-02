using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using WamBot.Twitch.Api;
using WamBot.Twitch.Data.SRC;

namespace WamBot.Twitch.Commands
{
    [Group("Speedrun.com", "Commands for interfacing with Speedrun.com", "src")]
    public class SRCCommands : CommandModule
    {
        private TwitchAPI _api;
        private IHttpClientFactory _httpClientFactory;
        private static Dictionary<string, string> _hardcodedGames = new Dictionary<string, string>()
        {
            ["Sonic the Hedgehog 4: Episode I"] = "s4e1",
            ["Sonic the Hedgehog 4: Episode II"] = "s4e2",
        };

        public SRCCommands(IHttpClientFactory httpClientFactory, TwitchAPI api)
        {
            _api = api;
            _httpClientFactory = httpClientFactory;
        }

        [Command("World Record", "Pulls the world record for a given game and category from Speedrun.com.", "wr")]
        public async Task WorldRecord(CommandContext ctx, string game = null, string category = null, params string[] variables)
        {
            var httpClient = _httpClientFactory.CreateClient("SRC");
            var srcGame = await FindGameAsync(game, ctx.Message.Channel, httpClient);
            if (srcGame == null)
            {
                ctx.Reply($"I couldn't find the game {game}!");
                return;
            }

            var srcCategory = await FindCategoryAsync(category, httpClient, srcGame);
            if (srcCategory == null)
            {
                if (category != null)
                    ctx.Reply($"I couldn't find a suitable full-game category called '{category}'!");
                else
                    ctx.Reply($"I couldn't find a suitable full-game category!");

                return;
            }

            var builder = new Dictionary<string,string> { { "top", "1" }, { "embed", "players" } };
            var srcVariables = await httpClient.GetSRCObjectAsync<List<SRCVariable>>($"categories/{srcCategory.Id}/variables");
            var usedVariables = FilterVariables(variables, srcVariables);
            foreach (var item in usedVariables)
            {
                builder.Add($"var-{item.Key.Id}", item.Value);
            }

            var leaderboard = await httpClient.GetSRCObjectAsync<SRCLeaderboard>(QueryHelpers.AddQueryString($"leaderboards/{srcGame.Id}/category/{srcCategory.Id}", builder));

            var run = leaderboard.Runs[0].Run;
            var time = TimeSpan.FromSeconds(run.Times.Primary);

            var variablesString = usedVariables.Any() ? $"({string.Join(", ", usedVariables.Select(v => v.Key.IsSubcategory ? v.Key.Values.Values[v.Value].Label : $"{v.Key.Name}: {v.Key.Values.Values[v.Value].Label}"))})" : "";

            var players = run.Players.Select(p => GetPlayerUsername(p, leaderboard.Players.Data)).ToArray();
            var playerString = players.Length == 1 ? players[0] : (string.Join(", ", players[..^1]) + " & " + players[^1]);
            var video = run.Videos?.Links.FirstOrDefault()?.Uri;
            var videoUrl = $"({FilterVideoUrl(video)})";

            ctx.Reply($"{srcGame.Names.International} - {srcCategory.Name} {variablesString} world record is {time:g} by {playerString} {videoUrl}");
        }

        private static string FilterVideoUrl(Uri video)
        {
            var videoUrl = "";
            if (video != null)
            {
                var query = QueryHelpers.ParseQuery(video.Query.TrimStart('?'));
                if (video.Host == "youtube.com" || video.Host == "www.youtube.com")
                {
                    videoUrl = "https://youtu.be/" + query["v"];
                    if (query.TryGetValue("t", out var ts))
                        videoUrl += "?t=" + ts;
                }
                else
                {
                    videoUrl = video.ToString();
                }
            }

            return videoUrl;
        }

        [Command("Variables", "Pulls the variables for a given game and category from Speedrun.com.")]
        public async Task VariablesAsync(CommandContext ctx, string game = null, string category = null)
        {
            var httpClient = _httpClientFactory.CreateClient("SRC");
            var srcGame = await FindGameAsync(game, ctx.Message.Channel, httpClient);
            if (srcGame == null)
            {
                ctx.Reply($"I couldn't find the game {game}!");
                return;
            }

            var srcCategory = await FindCategoryAsync(category, httpClient, srcGame);
            if (srcCategory == null)
            {
                if (category != null)
                    ctx.Reply($"I couldn't find a suitable full-game category called '{category}'!");
                else
                    ctx.Reply($"I couldn't find a suitable full-game category!");
                return;
            }

            var srcVariables = await httpClient.GetSRCObjectAsync<List<SRCVariable>>($"categories/{srcCategory.Id}/variables");
            var list = new List<string>();
            foreach (var variables in srcVariables.OrderByDescending(v => v.IsSubcategory))
            {
                if (variables.IsSubcategory)
                {
                    foreach (var value in variables.Values.Values.Values)
                    {
                        list.Add(value.Label);
                    }
                }
                else
                {
                    list.Add($"{variables.Name} ({string.Join("/", variables.Values.Values.Select(v => v.Value.Label))})");
                }
            }

            ctx.Reply($"{srcGame.Names.International} - {srcCategory.Name} has the following variables: {string.Join(", ", list)}");
        }

        [Command("\"World Record\"", "The totally legit actual SMO world record.", "realwr", "totallylegitwr")]
        public async Task FakeWorldRecord(CommandContext ctx)
        {
            var random = new Random();
            var httpClient = _httpClientFactory.CreateClient("SRC");
            var leaderboard = await httpClient.GetStringAsync("leaderboards/smo/category/Any");
            var parsedBoards = JObject.Parse(leaderboard);

            var runs = (JArray)parsedBoards["data"]["runs"];
            var index = (int)random.RandomBiasedPow(0, runs.Count, 8, runs.Count * 0.75d);
            var run = runs[index]["run"];
            var time = TimeSpan.FromSeconds(run["times"]["primary_t"].ToObject<double>());

            string playerName = await GetRunPlayserNameAsync(httpClient, run);

            ctx.Reply($"any% WR is {time:g} by {playerName}");
        }

        private async Task<SRCGame> FindGameAsync(string game, string channelName, HttpClient httpClient)
        {
            SRCGame srcGame = null;

            if (game == null)
            {
                var userId = (await _api.V5.Users.GetUserByNameAsync(channelName)).Matches[0].Id;
                game = (await _api.V5.Channels.GetChannelByIDAsync(userId)).Game;
            }

            if (_hardcodedGames.TryGetValue(game, out var hcGame))
            {
                game = hcGame;
            }

            try
            {
                srcGame = await httpClient.GetSRCObjectAsync<SRCGame>($"games/{game}");
            }
            catch
            {
                srcGame = (await httpClient.GetSRCObjectAsync<List<SRCGame>>("games?name=" + Uri.EscapeDataString(game))).FirstOrDefault();
            }

            return srcGame;
        }

        private static async Task<SRCCategory> FindCategoryAsync(string category, HttpClient httpClient, SRCGame srcGame)
        {
            var srcCategories = await httpClient.GetSRCObjectAsync<List<SRCCategory>>($"games/{srcGame.Id}/categories");
            SRCCategory srcCategory = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                srcCategory = srcCategories.OrderByDescending(c => string.Compare(c.Name, category, true, CultureInfo.InvariantCulture) == 0).FirstOrDefault(c => c.Type == "per-game");
            }
            else
            {
                srcCategory = srcCategories.FirstOrDefault(c => c.Type == "per-game");
            }

            return srcCategory;
        }

        private static Dictionary<SRCVariable, string> FilterVariables(string[] variables, List<SRCVariable> srcVariables)
        {
            var usedVariables = new Dictionary<SRCVariable, string>();
            foreach (var variable in srcVariables)
            {
                if (variable.IsSubcategory && variable.Values.Default != null)
                    usedVariables.Add(variable, variable.Values.Default);
            }

            foreach (var kvp in variables)
            {
                string name = null;
                string value = kvp;

                var idx = kvp.IndexOf('=');
                if (idx != -1)
                {
                    name = kvp.Substring(0, idx);
                    value = kvp.Substring(idx + 1);
                }

                foreach (var variable in srcVariables)
                {
                    if (name != null ? string.Compare(variable.Name, name, true) == 0 : variable.IsSubcategory)
                    {
                        var val = variable.Values.Values.FirstOrDefault(v => string.Compare(v.Value.Label, value, true) == 0);
                        if (val.Value != null)
                        {
                            usedVariables[variable] = val.Key;
                            continue;
                        }
                    }
                }
            }

            return usedVariables;
        }

        private static string GetPlayerUsername(SRCPlayer player, List<SRCPlayer> players)
        {
            if (player.Rel == "user")
            {
                return players.FirstOrDefault(p => p.Id == player.Id).Names.International;
            }

            return player.Name;
        }

        private static async Task<string> GetRunPlayserNameAsync(HttpClient httpClient, JToken run)
        {
            var playerName = "";
            var player = (JObject)(run["players"] as JArray)[0];
            if (player["rel"].ToString() == "user")
            {
                var playerText = await httpClient.GetStringAsync(player["uri"].ToString());
                player = JObject.Parse(playerText);
                playerName = player["data"]["names"]["international"].ToString();
            }
            else
            {
                playerName = player["name"].ToString();
            }

            return playerName;
        }
    }
}

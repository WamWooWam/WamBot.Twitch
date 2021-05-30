using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace WamBot.Twitch.Data.SRC
{
    public class SRCResponse<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public static class SRCExtensions
    {
        public static async Task<T> GetSRCObjectAsync<T>(this HttpClient client, string uri)
        {
            var resp = await client.GetStringAsync(uri);
            var data = JsonConvert.DeserializeObject<SRCResponse<T>>(resp);
            return data.Data;
        }
    }

    public class SRCPlayer
    {
        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("uri")]
        public Uri Uri { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("names")]
        public SRCNames Names { get; set; }
    }


    public class SRCNames
    {
        [JsonProperty("international")]
        public string International { get; set; }

        [JsonProperty("japanese")]
        public string Japanese { get; set; }

        [JsonProperty("twitch")]
        public string Twitch { get; set; }
    }

    public class SRCAsset
    {
        [JsonProperty("uri")]
        public Uri Uri { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class SRCLink
    {
        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("uri")]
        public Uri Uri { get; set; }
    }

    public class SRCRuleset
    {
        [JsonProperty("show-milliseconds")]
        public bool ShowMilliseconds { get; set; }

        [JsonProperty("require-verification")]
        public bool RequireVerification { get; set; }

        [JsonProperty("require-video")]
        public bool RequireVideo { get; set; }

        [JsonProperty("run-times")]
        public List<string> RunTimes { get; set; }

        [JsonProperty("default-time")]
        public string DefaultTime { get; set; }

        [JsonProperty("emulators-allowed")]
        public bool EmulatorsAllowed { get; set; }
    }

    public class SRCGame
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("names")]
        public SRCNames Names { get; set; }

        [JsonProperty("abbreviation")]
        public string Abbreviation { get; set; }

        [JsonProperty("weblink")]
        public Uri Weblink { get; set; }

        [JsonProperty("released")]
        public int Released { get; set; }

        [JsonProperty("release-date")]
        public DateTimeOffset? ReleaseDate { get; set; }

        [JsonProperty("ruleset")]
        public SRCRuleset Ruleset { get; set; }

        [JsonProperty("romhack")]
        public bool IsRomhack { get; set; }

        [JsonProperty("gametypes")]
        public List<string> Gametypes { get; set; }

        [JsonProperty("platforms")]
        public List<string> Platforms { get; set; }

        [JsonProperty("regions")]
        public List<string> Regions { get; set; }

        [JsonProperty("genres")]
        public List<string> Genres { get; set; }

        [JsonProperty("engines")]
        public List<string> Engines { get; set; }

        [JsonProperty("developers")]
        public List<string> Developers { get; set; }

        [JsonProperty("publishers")]
        public List<string> Publishers { get; set; }

        [JsonProperty("moderators")]
        public Dictionary<string, string> Moderators { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset? Created { get; set; }

        [JsonProperty("assets")]
        public Dictionary<string, SRCAsset> Assets { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }
    }

    public class SRCCategoryPlayers
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }

    public class SRCCategory
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("weblink")]
        public string Weblink { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("rules")]
        public string Rules { get; set; }

        [JsonProperty("players")]
        public SRCCategoryPlayers Players { get; set; }

        [JsonProperty("miscellaneous")]
        public bool? IsMiscellaneous { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }
    }

    public class SRCVariableScope
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }
    }

    public class SRCVariableValueFlags
    {
        [JsonProperty("miscellaneous")]
        public bool? IsMiscellaneous { get; set; }
    }

    public class SRCVariableValue
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("rules")]
        public string Rules { get; set; }

        [JsonProperty("flags")]
        public SRCVariableValueFlags Flags { get; set; }
    }

    public class SRCVariableValues
    {
        [JsonProperty("values")]
        public Dictionary<string, SRCVariableValue> Values { get; set; }

        [JsonProperty("default")]
        public string Default { get; set; }
    }

    public class SRCVariable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("scope")]
        public SRCVariableScope Scope { get; set; }

        [JsonProperty("mandatory")]
        public bool Mandatory { get; set; }

        [JsonProperty("user-defined")]
        public bool UserDefined { get; set; }

        [JsonProperty("obsoletes")]
        public bool Obsoletes { get; set; }

        [JsonProperty("values")]
        public SRCVariableValues Values { get; set; }

        [JsonProperty("is-subcategory")]
        public bool IsSubcategory { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }
    }

    public class SRCLeaderboardRun
    {
        [JsonProperty("place")]
        public int Place { get; set; }

        [JsonProperty("run")]
        public SRCRun Run { get; set; }
    }

    public class SRCLeaderboard
    {
        [JsonProperty("weblink")]
        public string Weblink { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("emulators")]
        public string Emulators { get; set; }

        [JsonProperty("video-only")]
        public bool VideoOnly { get; set; }

        [JsonProperty("timing")]
        public string Timing { get; set; }

        [JsonProperty("values")]
        public Dictionary<string, string> Values { get; set; }

        [JsonProperty("runs")]
        public List<SRCLeaderboardRun> Runs { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }

        [JsonProperty("players")]
        public SRCResponse<List<SRCPlayer>> Players { get; set; }
    }

    public class SRCSystem
    {
        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("emulated")]
        public bool Emulated { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }

    public class SRCVideos
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }
    }

    public class SRCTimes
    {
        [JsonProperty("primary_t")]
        public double Primary { get; set; }

        [JsonProperty("realtime_t")]
        public double Realtime { get; set; }

        [JsonProperty("realtime_noloads_t")]
        public double RealtimeWithoutLoads { get; set; }

        [JsonProperty("ingame_t")]
        public double Ingame { get; set; }
    }

    public class SRCRunStatus
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("examiner")]
        public string Examiner { get; set; }

        [JsonProperty("verify-date")]
        public DateTimeOffset? VerifyDate { get; set; }
    }

    public class SRCRun
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("weblink")]
        public string Weblink { get; set; }

        [JsonProperty("game")]
        public string Game { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("videos")]
        public SRCVideos Videos { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("status")]
        public SRCRunStatus Status { get; set; }

        [JsonProperty("players")]
        public List<SRCPlayer> Players { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset? Date { get; set; }

        [JsonProperty("submitted")]
        public DateTimeOffset? Submitted { get; set; }

        [JsonProperty("times")]
        public SRCTimes Times { get; set; }

        [JsonProperty("system")]
        public SRCSystem System { get; set; }

        [JsonProperty("splits")]
        public SRCLink Splits { get; set; }

        [JsonProperty("values")]
        public Dictionary<string, string> Values { get; set; }

        [JsonProperty("links")]
        public List<SRCLink> Links { get; set; }
    }

}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using WamBot.Twitch.Api;
using WamBot.Twitch.Converters;
using WamBot.Twitch.Data;
using WamBot.Twitch.Services;

namespace WamBot.Twitch
{
    public class Startup : IStartup
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration config, ILoggerFactory loggerFactory)
        {
            _configuration = config;
            _loggerFactory = loggerFactory;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient("SRC", c =>
            {
                c.BaseAddress = new Uri("https://www.speedrun.com/api/v1/");
                c.DefaultRequestHeaders.Add("X-API-Key", _configuration["SRC:ApiKey"]);
            });

            services.AddHttpClient("OpenElevation", c =>
                c.BaseAddress = new Uri("https://api.opentopodata.org/v1/"));

            services.AddDbContext<BotDbContext>(o => o.UseSqlite(_configuration["Database:ConnectionString"]), ServiceLifetime.Transient);

            using (var serviceCtx = services.BuildServiceProvider())
            using (var scope = serviceCtx.CreateScope())
            using (var database = scope.ServiceProvider.GetRequiredService<BotDbContext>())
            {
                database.Database.Migrate();

                var channels = database.DbChannels.Select(c => c.Name).ToList();
                channels.Add("wambot_");

                var credentials = new ConnectionCredentials(_configuration["Twitch:Username"], _configuration["Twitch:ChatAccessToken"]);
                var client = new TwitchClient(logger: _loggerFactory.CreateLogger<TwitchClient>());
                client.Initialize(credentials);
                client.AutoReListenOnException = true;
                services.AddSingleton(client);

                var api = new TwitchAPI(_loggerFactory);
                api.Settings.AccessToken = _configuration["Twitch:AccessToken"];
                api.Settings.ClientId = _configuration["Twitch:ClientId"];
                services.AddSingleton(api);

                var liveStreamMonitor = new LiveStreamMonitorService(api);
                liveStreamMonitor.SetChannelsByName(channels);
                liveStreamMonitor.Start();

                services.AddSingleton(liveStreamMonitor);
                services.AddSingleton<CommandRegistry>();
                services.AddParamConverter<User, TwitchUserConverter>();
                services.AddHostedService<BotService>();
                services.AddHostedService<EconomyService>();
                services.AddHostedService<WelcomeService>();
            }
        }

        public void Configure(IHostBuilder host)
        {

        }
    }
}

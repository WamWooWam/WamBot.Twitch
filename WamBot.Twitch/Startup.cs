using System;
using System.Linq;
using System.Threading.Tasks;
using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
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
using WamBot.Twitch.Interactivity;

namespace WamBot.Twitch
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration config)
        {
            _configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication((o) =>
            {
                o.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = TwitchAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(o =>
            {
                o.LoginPath = "/Auth";
                o.AccessDeniedPath = "/Auth/AccessDenied";
            })
            .AddTwitch(o =>
            {
                o.ClientId = _configuration["Twitch:AppClientId"];
                o.ClientSecret = _configuration["Twitch:AppClientSecret"];
            });

            services.AddAuthorization(o =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .RequireUserName("wamwoowam")
                                 .Build();
                o.AddPolicy("BotOwner", policy);
            });

            services.AddControllersWithViews(o =>
            {
                o.Filters.Add(new AuthorizeFilter("BotOwner"));
            });

            services.AddHttpClient("SRC", c =>
            {
                c.BaseAddress = new Uri("https://www.speedrun.com/api/v1/");
                c.DefaultRequestHeaders.Add("X-API-Key", _configuration["SRC:ApiKey"]);
            });

            services.AddHttpClient("OpenElevation", c =>
                c.BaseAddress = new Uri("https://api.opentopodata.org/v1/"));

            services.AddDbContext<BotDbContext>(o => o.UseNpgsql(_configuration["Database:ConnectionString"]), ServiceLifetime.Transient);
            

            services.AddSingleton((container) =>
            {
                var configuration = container.GetRequiredService<IConfiguration>();
                var loggerFactory = container.GetRequiredService<ILoggerFactory>();

                var credentials = new ConnectionCredentials(configuration["Twitch:Username"], configuration["Twitch:ChatAccessToken"]);
                var client = new TwitchClient(logger: loggerFactory.CreateLogger<TwitchClient>());
                client.Initialize(credentials);
                client.AutoReListenOnException = true;

                return client;
            });

            services.AddSingleton((container) =>
            {
                var loggerFactory = container.GetRequiredService<ILoggerFactory>();
                var api = new TwitchAPI(loggerFactory);
                api.Settings.AccessToken = _configuration["Twitch:AccessToken"];
                api.Settings.ClientId = _configuration["Twitch:ClientId"];
                return api;
            });

            services.AddSingleton((container) =>
            {
                var api = container.GetRequiredService<TwitchAPI>();
                return new LiveStreamMonitorService(api);
            });

            services.AddParamConverter<User, TwitchUserConverter>();
            
            services.AddSingleton<CommandRegistry>();
            services.AddHostedService<BotService>();
            services.AddHostedService<EconomyService>();
            services.AddHostedService<WelcomeService>();
        }

        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env, 
            BotDbContext dbContext)
        {
            dbContext.Database.Migrate();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

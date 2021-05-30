using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WamBot.Twitch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c => c.AddEnvironmentVariables("WAMTWITCH_").AddCommandLine(args))
                .ConfigureHostConfiguration(c => c.AddEnvironmentVariables("WAMTWITCH_").AddCommandLine(args))
                .UseSystemd()
                .Build<Startup>();

            await host.RunAsync();
        }
    }
}

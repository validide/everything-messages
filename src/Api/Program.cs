using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EverythingMessages.Api
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        internal static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.AddEnvironmentVariables(prefix: "EM_");
                    configHost.AddCommandLine(args);
                })
                .UseSerilog((host, log) =>
                {
                    if (host.HostingEnvironment.IsProduction())
                    {
                        log.MinimumLevel.Information();
                    }
                    else
                    {
                        log.MinimumLevel.Debug();
                    }

                    log.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
                    log.WriteTo.Console();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

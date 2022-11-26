using System;
using System.Configuration;
using EverythingMessages.Infrastructure;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using Serilog.Events;

namespace EverythingMessages.Scheduler;

internal static class Program
{
    private static bool? s_isRunningInContainer;
    internal static bool IsRunningInContainer =>
        s_isRunningInContainer ??= Boolean.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) && inContainer;

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
            .ConfigureServices((hostContext, services) =>
            {
                var epOptions = hostContext.Configuration.Get<EndpointConfigurationOptions>();
                services.AddSingleton(epOptions);
                services.AddOptions<MassTransitHostOptions>()
                    .Configure(options => options.WaitUntilStarted = epOptions.WaitBusStart);

                var schedulerEndpoint = String.IsNullOrEmpty(epOptions.SchedulerQueue)
                    ? null
                    : new Uri($"queue:{epOptions.SchedulerQueue}");

                services
                    .Configure<QuartzOptions>(hostContext.Configuration.GetSection("Quartz"))
                    // https://github.com/quartznet/quartznet/blob/v3.5.0/src/Quartz.Extensions.DependencyInjection/ServiceCollectionExtensions.cs#L80
                    .AddSingleton<ISchedulerFactory, ServiceCollectionSchedulerFactory>()
                    .AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());

                var messageBrokerHost = IsRunningInContainer ? "message-broker" : "localhost";
                var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                services.TryAddSingleton(nameFormatter);
                services.AddMassTransit(mt =>
                {
                    if (schedulerEndpoint != null)
                    {
                        mt.AddMessageScheduler(schedulerEndpoint);
                    }

                    mt.AddPublishMessageScheduler();
                    mt.AddQuartzConsumers();


                    mt.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(messageBrokerHost);
                        cfg.UsePublishMessageScheduler();
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            });
}

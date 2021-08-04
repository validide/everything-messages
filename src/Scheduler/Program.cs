﻿using System;
using EverythingMessages.Infrastructure;
using EverythingMessages.Infrastructure.MessageBus;
using EverythingMessages.Scheduler.Configuration;
using EverythingMessages.Scheduler.Definitions;
using EverythingMessages.Scheduler.Quartz;
using EverythingMessages.Scheduler.Quartz.Configuration;
using GreenPipes.Partitioning;
using MassTransit;
using MassTransit.Definition;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.Impl;
using Quartz.Simpl;
using Serilog;
using Serilog.Events;

namespace EverythingMessages.Scheduler
{
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
                    services.AddSingleton(hostContext.Configuration.Get<EndpointConfigurationOptions>());
                    services.AddSingleton(hostContext.Configuration.GetSection("Quartz").Get<QuartzOptions>());

                    var messageBrokerHost = IsRunningInContainer ? "message-broker" : "localhost";
                    var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                    services.TryAddSingleton(nameFormatter);
                    services.AddMassTransit(mt =>
                    {
                        mt.AddConsumer<ScheduleMessageConsumer, ScheduleMessageConsumerDefinition>();
                        mt.AddConsumer<CancelScheduledMessageConsumer, CancelScheduledMessageConsumerDefinition>();

                        mt.UsingRabbitMq((ctx, cfg) =>
                        {

                            cfg.Host(messageBrokerHost);
                            cfg.ConfigureEndpoints(ctx);

                            cfg.ConnectBusObserver(ctx.GetRequiredService<SchedulerBusObserver>());
                        });
                    });


                    services.AddSingleton<QuartzConfiguration>();

                    services.AddSingleton(new SchedulerOptions
                    {
                        PartitionCount = 16 // TODO: Move this configuration
                    });

                    services.AddSingleton<ISchedulerRepository>(s =>
                    {
                        return new ParitionedSchedulerRepository(
                            s.GetRequiredService<SchedulerOptions>(),
                            s.GetRequiredService<QuartzConfiguration>(),
                            new PropertySettingJobFactory(),
                            new Murmur3UnsafeHashGenerator(),
                            s.GetRequiredService<ILoggerFactory>()
                        );
                    });


                    services.AddSingleton<SchedulerBusObserver>();
                    services.AddSingleton<QuartzEndpointDefinition>();
                    services.AddHostedService<MassTransitHostedService>();
                });
    }
}

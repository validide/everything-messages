using System;
using System.Collections.Specialized;
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
                var epOptions = hostContext.Configuration.Get<EndpointConfigurationOptions>()!;
                services.AddSingleton(epOptions);
                services.AddOptions<MassTransitHostOptions>()
                    .Configure(options => options.WaitUntilStarted = epOptions.WaitBusStart);

                var schedulerEndpoint = String.IsNullOrEmpty(epOptions.SchedulerQueue)
                    ? null
                    : new Uri($"queue:{epOptions.SchedulerQueue}");

                var schedulerOptions = hostContext.Configuration.GetSection("SchedulerOptions").Get<SchedulerOptions>()!;
                services.AddSingleton(schedulerOptions);

                var instanceName = schedulerOptions.InstanceName ?? "MassTransit-Scheduler";
                var maxConcurrency = Environment.ProcessorCount * schedulerOptions.ConcurrencyMultiplier;
                var quartzOptions = new NameValueCollection
                {
                    {"quartz.scheduler.instanceName", instanceName},
                    {"quartz.scheduler.instanceId", "AUTO"},
                    {"quartz.plugin.timeZoneConverter.type", "Quartz.Plugin.TimeZoneConverter.TimeZoneConverterPlugin, Quartz.Plugins.TimeZoneConverter"},
                    {"quartz.serializer.type", "json"},
                    {"quartz.threadPool.maxConcurrency", maxConcurrency.ToString("F0")},
                    {"quartz.jobStore.misfireThreshold", "30000"},
                    {"quartz.jobStore.maxMisfiresToHandleAtATime", (maxConcurrency / 2).ToString("F0")},
                    {"quartz.jobStore.driverDelegateType", schedulerOptions.DriverDelegateType},
                    {"quartz.jobStore.tablePrefix", schedulerOptions.TablePrefix ?? "QRTZ_"},
                    {"quartz.jobStore.clustered", $"{schedulerOptions.Clustered ?? true}"},
                    {"quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz"},
                    {"quartz.jobStore.useProperties", "true"},
                    {"quartz.jobStore.dataSource", "default"},
                    {"quartz.dataSource.default.provider", schedulerOptions.Provider},
                    {"quartz.dataSource.default.connectionString", schedulerOptions.ConnectionString}
                };

                if (schedulerOptions.EnableBatching ?? false)
                {
                    quartzOptions.Add(new NameValueCollection
                    {
                        {"quartz.jobStore.acquireTriggersWithinLock", "true"},
                        {"quartz.scheduler.batchTriggerAcquisitionMaxCount", schedulerOptions.BatchSize.ToString("F0")},
                        {"quartz.scheduler.batchTriggerAcquisitionFireAheadTimeWindow", schedulerOptions.BatchHasten.ToString("F0")}
                    });
                }

                services
                    .AddQuartz(quartzOptions, q => q.UseMicrosoftDependencyInjectionJobFactory())
                    .AddQuartzHostedService(options =>
                    {
                        // when shutting down we want jobs to complete gracefully
                        options.WaitForJobsToComplete = true;
                    });

                var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                services.TryAddSingleton(nameFormatter);
                services.AddMassTransit(mt =>
                {
                    if (schedulerEndpoint != null)
                    {
                        mt.AddMessageScheduler(schedulerEndpoint);
                    }

                    mt.AddPublishMessageScheduler();
                    mt.AddQuartzConsumers(opt =>
                    {
                        opt.QueueName = epOptions.SchedulerQueue;
                        opt.PrefetchCount = 64;
                    });


                    mt.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(epOptions.GetMessageBrokerEndpoint());
                        cfg.UsePublishMessageScheduler();
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
            });
}

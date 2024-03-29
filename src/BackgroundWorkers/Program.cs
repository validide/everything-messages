﻿using System;
using EverythingMessages.Components.Notifications;
using EverythingMessages.Components.Orders;
using EverythingMessages.Infrastructure;
using EverythingMessages.Infrastructure.DocumentStore;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace EverythingMessages.BackgroundWorkers;

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
                var epConfig = hostContext.Configuration.Get<EndpointConfigurationOptions>()!;
                services.AddSingleton(epConfig);
                services.AddOptions<MassTransitHostOptions>()
                    .Configure(options => options.WaitUntilStarted = epConfig.WaitBusStart);

                var schedulerEndpoint = String.IsNullOrEmpty(epConfig.SchedulerQueue)
                    ? null
                    : new Uri($"queue:{epConfig.SchedulerQueue}");

                var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                services.TryAddSingleton(nameFormatter);
                services.AddMassTransit(mt =>
                {
                    if (schedulerEndpoint != null)
                    {
                        mt.AddMessageScheduler(schedulerEndpoint);
                    }

                    mt.AddConsumer<SubmitOrderConsumer, SubmitOrderConsumerDefinition>();
                    mt.AddConsumer<OrderSubmittedConsumer, OrderSubmittedConsumerDefinition>();
                    mt.AddConsumer<SendEmailNotificationConsumer, SendEmailNotificationConsumerDefinition>();

                    mt.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(epConfig.GetMessageBrokerEndpoint());
                        cfg.ConfigureEndpoints(ctx);

                        if (schedulerEndpoint != null)
                        {
                            cfg.UseMessageScheduler(schedulerEndpoint);
                        }
                    });
                });

                services.AddSingleton(new MongoDocumentStore.MongoDocumentStoreOptions
                {
                    Collection = "message-data",
                    Database = "short-term-storage",
                    Url = epConfig.GetDocumentStoreEndpoint()
                });
                services.AddScoped<IDocumentStore, MongoDocumentStore>();
                if (epConfig.Name.Contains("SCHEDULED_MESSAGE_PRODUCER", StringComparison.InvariantCultureIgnoreCase))
                {
                    services.AddHostedService<ScheduledMessagesProducerHostedService>();
                }
            });
}

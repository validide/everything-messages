using Microsoft.Extensions.Hosting;
using MassTransit;
using Serilog;
using Serilog.Events;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using EverythingMessages.Infrastructure.DocumentStore;
using EverythingMessages.Components.Orders;
using EverythingMessages.Infrastructure;
using Microsoft.Extensions.Configuration;
using EverythingMessages.Components.Notifications;
using EverythingMessages.BackgroundWorkers;

namespace EverythingMessages.Api
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
                    var epConfig = hostContext.Configuration.Get<EndpointConfigurationOptions>();
                    services.AddSingleton(epConfig);
                    services.AddOptions<MassTransitHostOptions>()
                        .Configure(options => options.WaitUntilStarted = epConfig.WaitBusStart);

                    var messageBrokerHost = IsRunningInContainer ? "message-broker" : "localhost";
                    var documentStoreHost = IsRunningInContainer ? "document-store" : "localhost";
                    var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                    services.TryAddSingleton(nameFormatter);
                    services.AddMassTransit(mt =>
                    {                
                        if (!String.IsNullOrEmpty(epConfig.SchedulerQueue))
                        {
                            mt.AddMessageScheduler(epConfig.SchedulerQueue);
                        }

                        mt.AddConsumer<SubmitOrderConsumer, SubmitOrderConsumerDefinition>();
                        mt.AddConsumer<OrderSubmittedConsumer, OrderSubmittedConsumerDefinition>();
                        mt.AddConsumer<SendEmailNotificationConsumer, SendEmailNotificationConsumerDefinition>();

                        mt.UsingRabbitMq((ctx, cfg) =>
                        {
                            cfg.Host(messageBrokerHost);
                            cfg.ConfigureEndpoints(ctx);

                            if (!String.IsNullOrEmpty(epConfig.SchedulerQueue))
                            {
                                cfg.UseMessageScheduler(epConfig.SchedulerQueue);
                            }
                        });
                    });

                    services.AddSingleton(new MongoDocumentStore.MongoDocumentStoreOptions
                    {
                        Collection = "message-data",
                        Database = "short-term-storage",
                        Url = $"mongodb://{documentStoreHost}:27017"
                    });
                    services.AddScoped<IDocumentStore, MongoDocumentStore>();
                    if (epConfig.Name.Contains("SCHEDULED_MESSAGE_PRODUCER", StringComparison.InvariantCultureIgnoreCase))
                    {
                        services.AddHostedService<ScheduledMessagesProducerHostedService>();
                    }
                });
    }
}

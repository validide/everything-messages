using Microsoft.Extensions.Hosting;
using MassTransit;
using Serilog;
using Serilog.Events;
using System;
using MassTransit.Definition;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using EverythingMessages.Infrastructure.DocumentStore;
using EverythingMessages.Components.Auditing;
using EverythingMessages.Infrastructure;

namespace EverythingMessages.BackgroundAuditor
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
                .ConfigureServices(services =>
                {
                    var messageBrokerHost = IsRunningInContainer ? "message-broker" : "localhost";
                    var documentStoreHost = IsRunningInContainer ? "document-store" : "localhost";
                    var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                    services.TryAddSingleton(nameFormatter);
                    services.AddMassTransit(mt =>
                    {
                        mt.AddConsumer<OrderAuditConsumer, OrderAuditConsumerDefinition>();

                        mt.UsingRabbitMq((ctx, cfg) =>
                        {
                            cfg.Host(messageBrokerHost, "em");
                            cfg.ConfigureEndpoints(ctx);
                        });
                    });

                    services.AddSingleton(new EndpointConfigurationOptions { Name = "auditor" });
                    services.AddSingleton(new MongoDocumentStore.MongoDocumentStoreOptions
                    {
                        Collection = "message-data",
                        Database = "short-term-storage",
                        Url = $"mongodb://{documentStoreHost}:27017"
                    });
                    services.AddScoped<IDocumentStore, MongoDocumentStore>();
                    services.AddHostedService<MassTransitHostedService>();
                });
    }
}

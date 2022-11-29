using Microsoft.Extensions.Hosting;
using MassTransit;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using EverythingMessages.Infrastructure.DocumentStore;
using EverythingMessages.Components.Auditing;
using EverythingMessages.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace EverythingMessages.BackgroundAuditor;

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

                var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
                services.TryAddSingleton(nameFormatter);
                services.AddMassTransit(mt =>
                {
                    mt.AddConsumer<OrderAuditConsumer, OrderAuditConsumerDefinition>();

                    mt.UsingRabbitMq((ctx, cfg) =>
                    {
                        cfg.Host(epOptions.GetMessageBrokerEndpoint());
                        cfg.ConfigureEndpoints(ctx);
                    });
                });
                services.AddSingleton(new MongoDocumentStore.MongoDocumentStoreOptions
                {
                    Collection = "message-data",
                    Database = "short-term-storage",
                    Url = epOptions.GetDocumentStoreEndpoint()
                });
                services.AddScoped<IDocumentStore, MongoDocumentStore>();
            });
}

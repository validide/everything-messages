using System;
using EverythingMessages.Api.Infrastructure.DocumentStore;
using EverythingMessages.Components.Notifications;
using EverythingMessages.Contracts.Orders;
using EverythingMessages.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Api;

public class Startup
{
    public IConfiguration Configuration { get; }
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var epOptions = Configuration.Get<EndpointConfigurationOptions>()!;
        services.AddSingleton(epOptions);

        var nameFormatter = SnakeCaseEndpointNameFormatter.Instance;
        services.TryAddSingleton(nameFormatter);
        services.AddMassTransit(mt =>
        {
            mt.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(epOptions.GetMessageBrokerEndpoint());
                cfg.ConfigureEndpoints(ctx);
            });

            // mt.AddRequestClient<SubmitOrder>(new Uri($"queue:{nameFormatter.Consumer<SubmitOrderConsumer>()}"));
            mt.AddRequestClient<SubmitOrder>();
            mt.AddRequestClient<CheckOrder>();
            mt.AddConsumer<OrderNotificationsConsumer, OrderNotificationsConsumerDefinition>();
        });

        services.Configure<HealthCheckPublisherOptions>(options =>
        {
            options.Delay = TimeSpan.FromSeconds(2);
            options.Predicate = check => check.Tags.Contains("ready");
        });

        services
            .AddControllers()
            .Services
            .AddSingleton(new MongoDocumentStore.MongoDocumentStoreOptions
            {
                Collection = "message-data",
                Database = "short-term-storage",
                Url = epOptions.GetDocumentStoreEndpoint()
            })
            .AddScoped<IDocumentStore, MongoDocumentStore>()
            .AddOpenApiDocument(cfg => cfg.PostProcess = d => d.Info.Title = "HTTP API V1");
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseOpenApi(cfg => {
            cfg.PostProcess = (doc, _) =>
            {
                if (serviceProvider.GetRequiredService<EndpointConfigurationOptions>().InContainer)
                {
                    doc.Host = "localhost:7000"; // we are passing through API gateway
                }
            };
        });
        app.UseSwaggerUi3(); // serve Swagger UI

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            // The readiness check uses all registered checks with the 'ready' tag.
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
            });

            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                // Exclude all checks and return a 200-Ok.
                Predicate = _ => false
            });
        });

        serviceProvider.GetRequiredService<ILogger<Startup>>()
            .LogInformation("API Started: {isDevelopment}/{environmentName}.", env.IsDevelopment(), env.EnvironmentName);
    }
}

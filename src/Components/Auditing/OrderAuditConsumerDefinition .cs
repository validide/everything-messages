using System;
using EverythingMessages.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingMessages.Components.Auditing;

public class OrderAuditConsumerDefinition : ConsumerDefinition<OrderAuditConsumer>
{
    private readonly IServiceProvider _serviceProvider;

    public OrderAuditConsumerDefinition(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ConcurrentMessageLimit = _serviceProvider.GetRequiredService<EndpointConfigurationOptions>().ConcurrentMessageLimit ?? 2;
        EndpointName = _serviceProvider.GetRequiredService<IEndpointNameFormatter>().Consumer<OrderAuditConsumer>();
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<OrderAuditConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, 1000));
        endpointConfigurator.UseServiceScope(_serviceProvider);
    }
}

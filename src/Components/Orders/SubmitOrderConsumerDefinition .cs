using System;
using EverythingMessages.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingMessages.Components.Orders;

public class SubmitOrderConsumerDefinition: ConsumerDefinition<SubmitOrderConsumer>
{
    private readonly IServiceProvider _serviceProvider;

    public SubmitOrderConsumerDefinition(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ConcurrentMessageLimit = _serviceProvider.GetRequiredService<EndpointConfigurationOptions>().ConcurrentMessageLimit ?? 2;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<SubmitOrderConsumer> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, 1000));
        endpointConfigurator.UseServiceScope(_serviceProvider);

        // consumerConfigurator.Message<SubmitOrder>(m => m.UseFilter(new ContainerScopedFilter()));
    }
}

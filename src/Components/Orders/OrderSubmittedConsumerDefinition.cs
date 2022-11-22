using System;
using EverythingMessages.Infrastructure;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace EverythingMessages.Components.Orders
{
    public class OrderSubmittedConsumerDefinition : ConsumerDefinition<OrderSubmittedConsumer>
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderSubmittedConsumerDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            ConcurrentMessageLimit = _serviceProvider.GetRequiredService<EndpointConfigurationOptions>().ConcurrentMessageLimit ?? 2;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<OrderSubmittedConsumer> consumerConfigurator)
        {
            // Keep the redelivery before the retry else the retry will not take place,
            // only the redelivery will happen
            endpointConfigurator.UseScheduledRedelivery(r => r.Interval(10, TimeSpan.FromSeconds(30)));
            endpointConfigurator.UseMessageRetry(r => r.Interval(2, TimeSpan.FromSeconds(5)));

            endpointConfigurator.UseServiceScope(_serviceProvider);
        }
    }
}

using System;
using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;

namespace EverythingMessages.Components.Orders
{
    public class OrderSubmittedConsumerDefinition : ConsumerDefinition<OrderSubmittedConsumer>
    {
        private readonly IServiceProvider _serviceProvider;

        public OrderSubmittedConsumerDefinition(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            ConcurrentMessageLimit = 2;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<OrderSubmittedConsumer> consumerConfigurator)
        {
            endpointConfigurator.UseMessageRetry(r => r.Interval(3, 1000));
            endpointConfigurator.UseServiceScope(_serviceProvider);

            //consumerConfigurator.Message<SubmitOrder>(m => m.UseFilter(new ContainerScopedFilter()));
        }
    }
}

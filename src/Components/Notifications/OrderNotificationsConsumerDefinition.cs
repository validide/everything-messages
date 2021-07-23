using System;
using EverythingMessages.Infrastructure;
using MassTransit;
using MassTransit.Definition;

namespace EverythingMessages.Components.Notifications
{
    public class OrderNotificationsConsumerDefinition: ConsumerDefinition<OrderNotificationsConsumer>
    {
        private readonly EndpointConfigurationOptions _endpointConfigurationOptions;
        private readonly IEndpointNameFormatter _endpointNameFormatter;
        public OrderNotificationsConsumerDefinition(EndpointConfigurationOptions endpointConfigurationOptions, IEndpointNameFormatter endpointNameFormatter)
        {
            _endpointConfigurationOptions = endpointConfigurationOptions;
            _endpointNameFormatter = endpointNameFormatter;
            ConcurrentMessageLimit = 1;
            Endpoint(e =>
            {
                e.InstanceId = _endpointNameFormatter.SanitizeName($"{_endpointConfigurationOptions.Name}_{Guid.NewGuid():N}");
                e.Temporary = true;
            });
        }
    }
}

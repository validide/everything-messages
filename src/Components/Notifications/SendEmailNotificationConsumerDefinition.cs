using EverythingMessages.Infrastructure;
using MassTransit.Definition;

namespace EverythingMessages.Components.Notifications
{
    public class SendEmailNotificationConsumerDefinition: ConsumerDefinition<SendEmailNotificationConsumer>
    {
        private readonly EndpointConfigurationOptions _endpointConfigurationOptions;
        public SendEmailNotificationConsumerDefinition(EndpointConfigurationOptions endpointConfigurationOptions)
        {
            _endpointConfigurationOptions = endpointConfigurationOptions;
            ConcurrentMessageLimit = (_endpointConfigurationOptions.ConcurrentMessageLimit ?? 1) * 20;
        }
    }
}

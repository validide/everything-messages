using GreenPipes;
using MassTransit;
using MassTransit.ConsumeConfigurators;
using MassTransit.Definition;
using MassTransit.QuartzIntegration;
using MassTransit.Scheduling;

namespace EverythingMessages.Scheduler.Definitions
{
    public class ScheduleMessageConsumerDefinition :
        ConsumerDefinition<ScheduleMessageConsumer>
    {
        private readonly QuartzEndpointDefinition _endpointDefinition;

        public ScheduleMessageConsumerDefinition(QuartzEndpointDefinition endpointDefinition)
        {
            _endpointDefinition = endpointDefinition;

            EndpointDefinition = _endpointDefinition;
        }

        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator,
            IConsumerConfigurator<ScheduleMessageConsumer> consumerConfigurator)
        {
            consumerConfigurator.Message<ScheduleMessage>(m => m.UsePartitioner(_endpointDefinition.Partition, p => p.Message.CorrelationId));
            endpointConfigurator.ConcurrentMessageLimit = _endpointDefinition.ConcurrentMessageLimit;
            if (_endpointDefinition.PrefetchCount!= null)
            {
                endpointConfigurator.PrefetchCount = _endpointDefinition.PrefetchCount.Value;
            }
            
        }
    }
}

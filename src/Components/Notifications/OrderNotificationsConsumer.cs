using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Notifications
{
    public class OrderNotificationsConsumer: IConsumer<OrderSubmitted>
    {
        private readonly ILogger<OrderNotificationsConsumer> _logger;

        public OrderNotificationsConsumer(ILogger<OrderNotificationsConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
            _logger.Log(LogLevel.Information, "Order {Id} submission was successfully processed.", context.Message.Id);
            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        }
    }
}

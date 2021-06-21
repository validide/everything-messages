using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Orders
{
    public class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
    {
        private readonly ILogger<OrderSubmittedConsumer> _logger;

        public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
            // Fake it ...
            _logger.Log(LogLevel.Information, "Processing order submision: {Id}", context.Message.Id);
            await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
            _logger.Log(LogLevel.Information, "Processed order submision: {Id}", context.Message.Id);

        }
    }
}

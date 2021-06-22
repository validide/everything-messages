using System;
using System.Text.Json;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Auditing
{
    public class OrderAuditConsumer : IConsumer<SubmitOrder>, IConsumer<OrderSubmitted>
    {
        private readonly ILogger<OrderAuditConsumer> _logger;

        public OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            _logger.Log(LogLevel.Information, "Auditing order: {Id}", context.Message.Id);


            await Task.Delay(TimeSpan.FromSeconds(13)).ConfigureAwait(false);

            _logger.Log(
                LogLevel.Information,
                "Audit details:\n\t-Message: {Message}\n\t-Headers: {Headers}",
                JsonSerializer.Serialize(context.Message),
                JsonSerializer.Serialize(context.Headers)
            );
        }

        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
            _logger.Log(LogLevel.Information, "Auditing order: {Id}", context.Message.Id);


            await Task.Delay(TimeSpan.FromSeconds(13)).ConfigureAwait(false);

            _logger.Log(
                LogLevel.Information,
                "Audit details:\n\t-Message: {Message}\n\t-Headers: {Headers}",
                JsonSerializer.Serialize(context.Message),
                JsonSerializer.Serialize(context.Headers)
            );
        }
    }
}

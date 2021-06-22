using System;
using System.Text.Json;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Auditing
{
    public class SubmitOrderAuditConsumer : IConsumer<SubmitOrder>
    {
        private readonly ILogger<SubmitOrderAuditConsumer> _logger;

        public SubmitOrderAuditConsumer(ILogger<SubmitOrderAuditConsumer> logger)
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
    }
}

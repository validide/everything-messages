using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Auditing
{
    public partial class OrderAuditConsumer : IConsumer<SubmitOrder>, IConsumer<OrderSubmitted>
    {
        [LoggerMessage(0, LogLevel.Information, "Auditing order submission: {Id}")]
        private static partial void LogAuditOrderSubmission(ILogger logger, string id);

        [LoggerMessage(1, LogLevel.Information, "Auditing submitted order: {Id}")]
        private static partial void LogAuditSubmittedOrder(ILogger logger, string id);

        [LoggerMessage(2, LogLevel.Information, "Audit details:\n\t-Message: {Message}\n\t-Headers: {Headers}")]
        private static partial void LogSubmitOrderAuditDetails(ILogger logger, SubmitOrder message, Headers headers);

        [LoggerMessage(3, LogLevel.Information, "Audit details:\n\t-Message: {Message}\n\t-Headers: {Headers}")]
        private static partial void LogOrderSubmittedAuditDetails(ILogger logger, OrderSubmitted message, Headers headers);

        private readonly ILogger<OrderAuditConsumer> _logger;

        public OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
        {
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            LogAuditOrderSubmission(_logger, context.Message.Id);

            await Task.Delay(TimeSpan.FromSeconds(13)).ConfigureAwait(false);
            LogSubmitOrderAuditDetails(_logger, context.Message, context.Headers);
        }

        public async Task Consume(ConsumeContext<OrderSubmitted> context)
        {
            LogAuditSubmittedOrder(_logger, context.Message.Id);

            await Task.Delay(TimeSpan.FromSeconds(13)).ConfigureAwait(false);

            LogOrderSubmittedAuditDetails(_logger, context.Message, context.Headers);
        }
    }
}

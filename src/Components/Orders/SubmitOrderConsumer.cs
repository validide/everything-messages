using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using EverythingMessages.Infrastructure.DocumentStore;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Orders
{
    public partial class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        [LoggerMessage(0, LogLevel.Information, "SubmitOrderConsumer: {id}")]
        private static partial void LogOrderSubmitted(ILogger logger, string id);

        [LoggerMessage(1, LogLevel.Information, "Processing order data: {data}")]
        private static partial void LogProcessingOrderData(ILogger logger, byte[] data);

        [LoggerMessage(2, LogLevel.Information, "Rejecting order: {id}")]
        private static partial void LogRejectingOrder(ILogger logger, string id);

        [LoggerMessage(3, LogLevel.Information, "Rejected order: {id}")]
        private static partial void LogOrderRejected(ILogger logger, string id);

        [LoggerMessage(4, LogLevel.Information, "Removed order data: {id}")]
        private static partial void LogRemovedOrderData(ILogger logger, string id);

        [LoggerMessage(5, LogLevel.Information, "Failed to clear order data: {id}")]
        private static partial void LogFailedToClearOrderData(ILogger logger, Exception ex, string id);

        private readonly ILogger<SubmitOrderConsumer> _logger;
        private readonly IDocumentStore _documentStore;

        public SubmitOrderConsumer(IDocumentStore documentStore, ILogger<SubmitOrderConsumer> logger)
        {
            _documentStore = documentStore;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            LogOrderSubmitted(_logger, context.Message.Id);

            var orderData = await _documentStore.GetAsync(context.Message.Id, context.CancellationToken).ConfigureAwait(false);
            LogProcessingOrderData(_logger, orderData);

            await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);


            if (String.IsNullOrEmpty(context.Message.CustomerId))
            {
                LogRejectingOrder(_logger, context.Message.Id);
                // Failed request
                if (context.RequestId != null)
                {
                    // Since we have the RequestId it means this was a Request/Response kind of call
                    await context.RespondAsync(new OrderSubmissionRejected { Code = "INVALID_CUSTOMER" }).ConfigureAwait(false);
                    LogRejectingOrder(_logger, context.Message.Id);
                }
            }
            else
            {
                await context.Publish(new OrderSubmitted
                {
                    Id = context.Message.Id,
                    CustomerId = context.Message.CustomerId,
                    Timestamp = DateTime.UtcNow
                }).ConfigureAwait(false);


                if (context.RequestId != null)
                {
                    await context.RespondAsync(new OrderSubmissionAccepted
                    {
                        Id = context.Message.Id,
                        CustomerId = context.Message.CustomerId
                    }).ConfigureAwait(false);
                }
            }

            try
            {
                await _documentStore.RemoveAsync(context.Message.Id, context.CancellationToken).ConfigureAwait(false);
                LogRemovedOrderData(_logger, context.Message.Id);
            }
            catch (Exception e)
            {
                // We don't really care about this as the document store should periodically clear data older than X. 
                LogFailedToClearOrderData(_logger, e, context.Message.Id);
            }
        }
    }
}

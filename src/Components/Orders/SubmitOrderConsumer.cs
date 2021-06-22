using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using EverythingMessages.Infrastructure.DocumentStore;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Orders
{
    public class SubmitOrderConsumer : IConsumer<SubmitOrder>
    {
        private readonly ILogger<SubmitOrderConsumer> _logger;
        private readonly IDocumentStore _documentStore;

        public SubmitOrderConsumer(IDocumentStore documentStore, ILogger<SubmitOrderConsumer> logger)
        {
            _documentStore = documentStore;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubmitOrder> context)
        {
            _logger.Log(LogLevel.Information, "SubmitOrderConsumer: {Id}", context.Message.Id);

            var orderData = await _documentStore.GetAsync(context.Message.Id, context.CancellationToken).ConfigureAwait(false);
            _logger.Log(LogLevel.Information, "Processing order data: {Data}", orderData);

            await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);


            if (String.IsNullOrEmpty(context.Message.CustomerId))
            {
                _logger.Log(LogLevel.Information, "Rejecting order: {Id}", context.Message.Id);
                // Failed request
                if (context.RequestId != null)
                {
                    // Since we have the RequestId it means this was a Request/Response kind of call
                    await context.RespondAsync(new OrderSubmissionRejected { Code = "INVALID_CUSTOMER" }).ConfigureAwait(false);
                    _logger.Log(LogLevel.Information, "Order rejected: {Id}", context.Message.Id);
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
                _logger.Log(LogLevel.Information, "Removed order data: {Id}", context.Message.Id);
            }
            catch (Exception e)
            {
                // We don't really care about this as the document store should periodically clear data older than X. 
                _logger.Log(LogLevel.Error, e, "Failed to clear order data: {Id}", context.Message.Id);
            }
        }
    }
}

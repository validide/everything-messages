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
            var retryAttempt = context.GetRetryAttempt();
            var RedeliveryCount = context.GetRedeliveryCount();

            _logger.Log(LogLevel.Information, "OrderSubmittedConsumer.Consume - RT: {retryAttempt} / RD: {RedeliveryCount}", retryAttempt, RedeliveryCount);

            // Fake it ...
            _logger.Log(LogLevel.Information, "Processing order submission: {Id}", context.Message.Id);
            if (retryAttempt > 0)
            {
                _logger.Log(LogLevel.Warning, "Attempting for the {retryAttempt} time to process order submission: {Id}", retryAttempt, context.Message.Id);
            }
            if (context.Message.CustomerId?.Contains("RETRY", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                if (RedeliveryCount > 4)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                    _logger.Log(LogLevel.Information, "Processed order submission on {RedeliveryCount} redelivery: {Id}", RedeliveryCount, context.Message.Id);
                }
                else
                {
                    var errorMessage = $"Customer {context.Message.CustomerId} is unlucky.";
                    _logger.Log(LogLevel.Error, errorMessage);
                    throw new ApplicationException(errorMessage);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
                _logger.Log(LogLevel.Information, "Processed order submission: {Id}", context.Message.Id);
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Orders;

public partial class OrderSubmittedConsumer : IConsumer<OrderSubmitted>
{
    [LoggerMessage(0, LogLevel.Information, "OrderSubmittedConsumer.Consume - RT: {retryAttempt} / RD: {RedeliveryCount}")]
    private static partial void LogRetry(ILogger logger, int retryAttempt, int redeliveryCount);

    [LoggerMessage(1, LogLevel.Information, "Processed order submission on {RedeliveryCount} redelivery: {Id}")]
    private static partial void LogRetryProcessed(ILogger logger, int redeliveryCount, string id);

    [LoggerMessage(2, LogLevel.Information, "Processed order submission: {Id}")]
    private static partial void LogProcessedOrder(ILogger logger, string id);

    [LoggerMessage(3, LogLevel.Information, "Processing order submission: {Id}")]
    private static partial void LogProcessingOrder(ILogger logger, string id);

    [LoggerMessage(4, LogLevel.Warning, "Attempting for the {retryAttempt} time to process order submission: {Id}")]
    private static partial void LogRetryAttempt(ILogger logger, int retryAttempt, string id);

    [LoggerMessage(5, LogLevel.Error, "Customer {customerId} is unlucky.")]
    private static partial void LogUnluckyCustomer(ILogger logger, string customerId);

    private readonly ILogger<OrderSubmittedConsumer> _logger;

    public OrderSubmittedConsumer(ILogger<OrderSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        var retryAttempt = context.GetRetryAttempt();
        var RedeliveryCount = context.GetRedeliveryCount();

        LogRetry(_logger, retryAttempt, RedeliveryCount);

        // Fake it ...
        LogProcessingOrder(_logger, context.Message.Id);
        if (retryAttempt > 0)
        {
            LogRetryAttempt(_logger, retryAttempt, context.Message.Id);
        }
        if (context.Message.CustomerId?.Contains("RETRY", StringComparison.InvariantCultureIgnoreCase) ?? false)
        {
            if (RedeliveryCount > 4)
            {
                await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
                LogRetryProcessed(_logger, RedeliveryCount, context.Message.Id);
            }
            else
            {
                LogUnluckyCustomer(_logger, context.Message.CustomerId);
                throw new ApplicationException($"Customer {context.Message.CustomerId} is unlucky.");
            }
        }
        else
        {
            await Task.Delay(TimeSpan.FromSeconds(7)).ConfigureAwait(false);
            LogProcessedOrder(_logger, context.Message.Id);
        }
    }
}

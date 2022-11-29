using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Notifications;

public partial class OrderNotificationsConsumer: IConsumer<OrderSubmitted>
{
    private readonly ILogger<OrderNotificationsConsumer> _logger;
    [LoggerMessage(0, LogLevel.Information, "Order {Id} submission was successfully processed.")]
    private static partial void LogOrderSubmission(ILogger logger, string id);

    public OrderNotificationsConsumer(ILogger<OrderNotificationsConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderSubmitted> context)
    {
        LogOrderSubmission(_logger, context.Message.Id);
        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

    }
}

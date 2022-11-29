using System;
using System.Threading;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Notifications;

public partial class SendEmailNotificationConsumer : IConsumer<SendEmailNotification>
{
    private readonly ILogger<SendEmailNotificationConsumer> _logger;
    private readonly Random _rng = new();
    private static int _messagesSent = 0; 

    [LoggerMessage(0, LogLevel.Information, "[{date}] Sending e-mail notification: {content}")]
    private static partial void LogEmailContent(ILogger logger, DateTime date, string content);

    public SendEmailNotificationConsumer(ILogger<SendEmailNotificationConsumer> logger) => _logger = logger;

    public Task Consume(ConsumeContext<SendEmailNotification> context)
    {
        if (Interlocked.Increment(ref _messagesSent) > 100)
        {
            LogEmailContent(_logger, DateTime.UtcNow, context.Message.Body);
            _ = Interlocked.Exchange(ref _messagesSent, 0);
        }

        return Task.CompletedTask;
    }
}

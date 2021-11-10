using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Notifications
{
    public partial class SendEmailNotificationConsumer : IConsumer<SendEmailNotification>
    {
        private readonly ILogger<SendEmailNotificationConsumer> _logger;
        private readonly Random _rng = new Random();

        [LoggerMessage(0, LogLevel.Information, "[{date}] Sending e-mail notification: {content}")]
        private static partial void LogEmailContent(ILogger logger, DateTime date, string content);

        public SendEmailNotificationConsumer(ILogger<SendEmailNotificationConsumer> logger) => _logger = logger;

        public Task Consume(ConsumeContext<SendEmailNotification> context)
        {
            if (context.Message.EmailAddress.StartsWith(_rng.Next(1, 10000).ToString("F0"), StringComparison.InvariantCultureIgnoreCase))
            {
                LogEmailContent(_logger, DateTime.UtcNow, context.Message.Body);
            }

            return Task.CompletedTask;
        }
    }
}

using System;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Components.Notifications
{
    public class SendEmailNotificationConsumer : IConsumer<SendEmailNotification>
    {
        private readonly ILogger<SendEmailNotificationConsumer> _logger;
        private readonly Random _rng = new Random();

        public SendEmailNotificationConsumer(ILogger<SendEmailNotificationConsumer> logger) => _logger = logger;

        public Task Consume(ConsumeContext<SendEmailNotification> context)
        {
            if (context.Message.EmailAddress.StartsWith(_rng.Next(1, 10000).ToString("F0"), StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation($"[{DateTime.UtcNow:O}] Sending e-mail notification: {context.Message.Body}");
            }

            return Task.CompletedTask;
        }
    }
}

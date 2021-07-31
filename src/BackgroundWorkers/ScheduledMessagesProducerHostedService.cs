using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Notifications;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace EverythingMessages.BackgroundWorkers
{
    internal class ScheduledMessagesProducerHostedService : BackgroundService
    {
        private readonly IMessageScheduler _messageScheduler;

        public ScheduledMessagesProducerHostedService(IMessageScheduler messageScheduler) => _messageScheduler = messageScheduler;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var dueDate = DateTime.UtcNow.AddSeconds(10);
                foreach (var customerId in Enumerable.Range(1, 1000))
                {
                    await _messageScheduler.SchedulePublish(
                        dueDate,
                        new SendEmailNotification
                        {
                            EmailAddress = $"{customerId}@example.com",
                            Body = $"Message for {customerId}@example.com at {dueDate:O}"
                        },
                        stoppingToken
                    ).ConfigureAwait(false);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EverythingMessages.Contracts.Notifications;
using EverythingMessages.Infrastructure.ExtensionMethods;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace EverythingMessages.BackgroundWorkers
{
    internal class ScheduledMessagesProducerHostedService : BackgroundService
    {
        private readonly IMessageScheduler _messageScheduler;

        public ScheduledMessagesProducerHostedService(IMessageScheduler messageScheduler)
        {
            _messageScheduler = messageScheduler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Wait for the queue to start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).ConfigureAwait(false);

            while (!stoppingToken.IsCancellationRequested)
            {
                var dueDate = DateTime.UtcNow.AddSeconds(10);
                await Enumerable.Range(1, 1000).ParallelForEachAsync((id, ct) =>
                    _messageScheduler.SchedulePublish(
                        dueDate,
                        new SendEmailNotification
                        {
                            EmailAddress = $"{id}@example.com",
                            Body = $"Message for {id}@example.com at {dueDate:O}"
                        },
                        ct)
                    ,Environment.ProcessorCount * 8, stoppingToken).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}

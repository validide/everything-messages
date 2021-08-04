using System;
using System.Threading.Tasks;
using EverythingMessages.Scheduler.Quartz.Configuration;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EverythingMessages.Scheduler.Quartz
{
    public class CancelScheduledMessageConsumer :
        IConsumer<CancelScheduledMessage>,
        IConsumer<CancelScheduledRecurringMessage>
    {
        private readonly SchedulerOptions _schedulerOptions;
        private readonly SchedulerBusObserver _schedulerBusObserver;
        private readonly ILogger<CancelScheduledMessageConsumer> _logger;

        public CancelScheduledMessageConsumer(SchedulerBusObserver schedulerBusObserver, ILogger<CancelScheduledMessageConsumer> logger, SchedulerOptions schedulerOptions)
        {
            _schedulerBusObserver = schedulerBusObserver;
            _logger = logger;
            _schedulerOptions = schedulerOptions;
        }

        public async Task Consume(ConsumeContext<CancelScheduledMessage> context)
        {
            var correlationId = context.Message.TokenId.ToString("N");

            var jobKey = new JobKey(correlationId);

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(correlationId);

            var deletedJob = await scheduler.DeleteJob(jobKey, context.CancellationToken).ConfigureAwait(false);

            if (deletedJob)
                _logger.LogDebug("Canceled Scheduled Message: {Id} at {Timestamp}", jobKey, context.Message.Timestamp);
            else
                _logger.LogDebug("CancelScheduledMessage: no message found for {Id}", jobKey);
        }

        public async Task Consume(ConsumeContext<CancelScheduledRecurringMessage> context)
        {
            var scheduleId = context.Message.ScheduleId;
            var scheduleGroup = context.Message.ScheduleGroup;

            if (!scheduleId.StartsWith(_schedulerOptions.RecurringTriggerPrefix))
                scheduleId = String.Concat(_schedulerOptions.RecurringTriggerPrefix, scheduleId);

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(String.Concat(scheduleId, scheduleGroup));

            var unscheduledJob = await scheduler.UnscheduleJob(new TriggerKey(scheduleId, context.Message.ScheduleGroup), context.CancellationToken)
                .ConfigureAwait(false);

            if (unscheduledJob)
            {
                _logger.LogDebug("CancelRecurringScheduledMessage: {ScheduleId}/{ScheduleGroup} at {Timestamp}", context.Message.ScheduleId,
                    context.Message.ScheduleGroup, context.Message.Timestamp);
            }
            else
            {
                _logger.LogDebug("CancelRecurringScheduledMessage: no message found {ScheduleId}/{ScheduleGroup}", context.Message.ScheduleId,
                    context.Message.ScheduleGroup);
            }
        }
    }
}

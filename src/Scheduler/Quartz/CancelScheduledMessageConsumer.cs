using System;
using System.Threading.Tasks;
using EverythingMessages.Scheduler.Quartz.Configuration;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Logging;
using Quartz;

namespace EverythingMessages.Scheduler.Quartz
{
    public partial class CancelScheduledMessageConsumer :
        IConsumer<CancelScheduledMessage>,
        IConsumer<CancelScheduledRecurringMessage>
    {
        [LoggerMessage(0, LogLevel.Debug, "Canceled Scheduled Message: {id} at {timestamp}")]
        private static partial void LogCancellation(ILogger logger, JobKey id, DateTime timestamp);

        [LoggerMessage(1, LogLevel.Error, "CancelScheduledMessage: no message found for {id}")]
        private static partial void LogCancellationFailure(ILogger logger, JobKey id);

        [LoggerMessage(2, LogLevel.Debug, "CancelRecurringScheduledMessage: {scheduleId}/{scheduleGroup} at {timestamp}")]
        private static partial void LogCancellationRecurring(ILogger logger, string scheduleId, string scheduleGroup, DateTime timestamp);

        [LoggerMessage(3, LogLevel.Error, "CancelRecurringScheduledMessage: no message found {ScheduleId}/{ScheduleGroup}")]
        private static partial void LogCancellationFailureRecurring(ILogger logger, string scheduleId, string scheduleGroup);

        private readonly SchedulerBusObserver _schedulerBusObserver;
        private readonly ILogger<CancelScheduledMessageConsumer> _logger;

        public CancelScheduledMessageConsumer(SchedulerBusObserver schedulerBusObserver, ILogger<CancelScheduledMessageConsumer> logger)
        {
            _schedulerBusObserver = schedulerBusObserver;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CancelScheduledMessage> context)
        {
            var correlationId = context.Message.TokenId.ToString("N");

            var jobKey = new JobKey(correlationId);

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(correlationId);

            var deletedJob = await scheduler.DeleteJob(jobKey, context.CancellationToken).ConfigureAwait(false);

            if (deletedJob)
                LogCancellation(_logger, jobKey, context.Message.Timestamp);
            else
                LogCancellationFailure(_logger, jobKey);
        }

        public async Task Consume(ConsumeContext<CancelScheduledRecurringMessage> context)
        {
            var scheduleId = context.Message.ScheduleId;
            var scheduleGroup = context.Message.ScheduleGroup;

            if (!scheduleId.StartsWith(SchedulerConstants.RecurringTriggerPrefix))
                scheduleId = String.Concat(SchedulerConstants.RecurringTriggerPrefix, scheduleId);

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(String.Concat(scheduleId, scheduleGroup));

            var unscheduledJob = await scheduler.UnscheduleJob(new TriggerKey(scheduleId, context.Message.ScheduleGroup), context.CancellationToken)
                .ConfigureAwait(false);

            if (unscheduledJob)
            {
                LogCancellationRecurring(_logger, context.Message.ScheduleId, context.Message.ScheduleGroup, context.Message.Timestamp);
            }
            else
            {
                LogCancellationFailureRecurring(_logger,context.Message.ScheduleId, context.Message.ScheduleGroup);
            }
        }
    }
}

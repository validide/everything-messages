using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EverythingMessages.Scheduler.Quartz.Configuration;
using MassTransit;
using MassTransit.Scheduling;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quartz;
using Quartz.Util;

namespace EverythingMessages.Scheduler.Quartz
{
    public partial class ScheduleMessageConsumer :
        IConsumer<ScheduleMessage>,
        IConsumer<ScheduleRecurringMessage>
    {
        [LoggerMessage(0, LogLevel.Debug, "Scheduled: {Key} {Schedule}")]
        private static partial void LogJobScheduled(ILogger logger, JobKey key, DateTimeOffset? schedule);

        private readonly SchedulerBusObserver _schedulerBusObserver;
        private readonly ILogger<ScheduleMessageConsumer> _logger;

        public ScheduleMessageConsumer(SchedulerBusObserver schedulerBusObserver, ILogger<ScheduleMessageConsumer> logger)
        {
            _schedulerBusObserver = schedulerBusObserver;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ScheduleMessage> context)
        {
            var correlationId = context.Message.CorrelationId.ToString("N");

            var jobKey = new JobKey(correlationId);

            var jobDetail = CreateJobDetail(context, context.Message.Destination, jobKey, context.Message.CorrelationId);

            var triggerKey = new TriggerKey(correlationId);
            var trigger = TriggerBuilder.Create()
                .ForJob(jobDetail)
                .StartAt(context.Message.ScheduledTime)
                .WithSchedule(SimpleScheduleBuilder.Create().WithMisfireHandlingInstructionFireNow())
                .WithIdentity(triggerKey)
                .Build();

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(correlationId);

            if (await scheduler.CheckExists(trigger.Key, context.CancellationToken).ConfigureAwait(false))
                await scheduler.UnscheduleJob(trigger.Key, context.CancellationToken).ConfigureAwait(false);

            await scheduler.ScheduleJob(jobDetail, trigger, context.CancellationToken).ConfigureAwait(false);

            LogJobScheduled(_logger, jobKey, trigger.GetNextFireTimeUtc());
        }

        public async Task Consume(ConsumeContext<ScheduleRecurringMessage> context)
        {
            var scheduleId = context.Message.Schedule.ScheduleId;
            var scheduleGroup = context.Message.Schedule.ScheduleGroup;
            var jobKey = new JobKey(scheduleId, context.Message.Schedule.ScheduleGroup);

            var jobDetail = CreateJobDetail(context, context.Message.Destination, jobKey);

            var triggerKey = new TriggerKey(String.Concat(SchedulerConstants.RecurringTriggerPrefix, scheduleId), scheduleGroup);

            var trigger = CreateTrigger(context.Message.Schedule, jobDetail, triggerKey);

            var scheduler = (await _schedulerBusObserver.GetSchedulerRepository().ConfigureAwait(false))
                .GetScheduler(String.Concat(scheduleId, scheduleGroup));

            if (await scheduler.CheckExists(triggerKey, context.CancellationToken).ConfigureAwait(false))
                await scheduler.UnscheduleJob(triggerKey, context.CancellationToken).ConfigureAwait(false);

            await scheduler.ScheduleJob(jobDetail, trigger, context.CancellationToken).ConfigureAwait(false);

            LogJobScheduled(_logger, jobKey, trigger.GetNextFireTimeUtc());
        }

        private ITrigger CreateTrigger(RecurringSchedule schedule, IJobDetail jobDetail, TriggerKey triggerKey)
        {
            var tz = TimeZoneInfo.Local;
            if (!String.IsNullOrWhiteSpace(schedule.TimeZoneId) && schedule.TimeZoneId != tz.Id)
                tz = TimeZoneUtil.FindTimeZoneById(schedule.TimeZoneId);

            var triggerBuilder = TriggerBuilder.Create()
                .ForJob(jobDetail)
                .WithIdentity(triggerKey)
                .StartAt(schedule.StartTime)
                .WithDescription(schedule.Description)
                .WithCronSchedule(schedule.CronExpression, x =>
                {
                    x.InTimeZone(tz);
                    switch (schedule.MisfirePolicy)
                    {
                        case MissedEventPolicy.Skip:
                            x.WithMisfireHandlingInstructionDoNothing();
                            break;

                        case MissedEventPolicy.Send:
                            x.WithMisfireHandlingInstructionFireAndProceed();
                            break;
                    }
                });

            if (schedule.EndTime.HasValue)
                triggerBuilder.EndAt(schedule.EndTime);

            return triggerBuilder.Build();
        }

        private static IJobDetail CreateJobDetail(ConsumeContext context, Uri destination, JobKey jobKey, Guid? tokenId = default)
        {
            var body = Encoding.UTF8.GetString(context.ReceiveContext.GetBody());

            var mediaType = context.ReceiveContext.ContentType?.MediaType;

            if (JsonMessageSerializer.JsonContentType.MediaType.Equals(mediaType, StringComparison.OrdinalIgnoreCase))
                body = TranslateJsonBody(body, destination.ToString());
            else
                throw new InvalidOperationException("Only JSON messages can be scheduled");

            var builder = JobBuilder.Create<ScheduledMessageJob>()
                .RequestRecovery()
                .WithIdentity(jobKey)
                .UsingJobData("Destination", ToString(destination))
                .UsingJobData("ResponseAddress", ToString(context.ResponseAddress))
                .UsingJobData("FaultAddress", ToString(context.FaultAddress))
                .UsingJobData("Body", body)
                .UsingJobData("ContentType", mediaType);

            if (context.MessageId.HasValue)
                builder = builder.UsingJobData("MessageId", context.MessageId.Value.ToString());

            if (context.CorrelationId.HasValue)
                builder = builder.UsingJobData("CorrelationId", context.CorrelationId.Value.ToString());

            if (context.ConversationId.HasValue)
                builder = builder.UsingJobData("ConversationId", context.ConversationId.Value.ToString());

            if (context.InitiatorId.HasValue)
                builder = builder.UsingJobData("InitiatorId", context.InitiatorId.Value.ToString());

            if (context.RequestId.HasValue)
                builder = builder.UsingJobData("RequestId", context.RequestId.Value.ToString());

            if (context.ExpirationTime.HasValue)
                builder = builder.UsingJobData("ExpirationTime", context.ExpirationTime.Value.ToString("O"));

            if (tokenId.HasValue)
                builder = builder.UsingJobData("TokenId", tokenId.Value.ToString("N"));

            var headers = context.Headers.GetAll();
            if (headers.Any())
                builder = builder.UsingJobData("HeadersAsJson", JsonConvert.SerializeObject(headers));

            return builder.Build();
        }

        private static string ToString(Uri uri)
        {
            return uri?.ToString() ?? "";
        }

        private static string TranslateJsonBody(string body, string destination)
        {
            var envelope = JObject.Parse(body);

            envelope["destinationAddress"] = destination;

            var message = envelope["message"];

            var payload = message["payload"];
            var payloadType = message["payloadType"];

            envelope["message"] = payload;
            envelope["messageType"] = payloadType;

            return JsonConvert.SerializeObject(envelope, Formatting.Indented);
        }
    }
}

﻿using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Serialization;
using Microsoft.Extensions.Logging;
using Quartz;


namespace EverythingMessages.Scheduler.Quartz
{
    public partial class ScheduledMessageJob :
        IJob,
        SerializedMessage
    {
        [LoggerMessage(0, LogLevel.Debug, "Schedule Executed: {key} {schedule}")]
        private static partial void LogScheduleExecuted(ILogger logger, JobKey key, DateTimeOffset? schedule);

        [LoggerMessage(1, LogLevel.Error, "Failed to send scheduled message, type: {messageType}, destination: {destinationAddress}")]
        private static partial void LogFailure(ILogger logger, string messageType, string destinationAddress);

        private readonly IBus _bus;
        private readonly ILogger<ScheduledMessageJob> _logger;

        public ScheduledMessageJob(IBus bus, ILogger<ScheduledMessageJob> logger)
        {
            _bus = bus;
            _logger = logger;
        }

        public string Destination { get; set; }
        public string MessageType { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var destinationAddress = new Uri(Destination);
                var sourceAddress = _bus.Address;

                var sendPipe = CreateMessageContext(sourceAddress);

                var endpoint = await _bus.GetSendEndpoint(destinationAddress).ConfigureAwait(false);

                var scheduled = new Scheduled();

                await endpoint.Send(scheduled, sendPipe, context.CancellationToken).ConfigureAwait(false);
                LogScheduleExecuted(_logger, context.JobDetail.Key, context.Trigger.GetNextFireTimeUtc());
            }
            catch (Exception ex)
            {
                LogFailure(_logger, MessageType, Destination);

                throw new JobExecutionException(ex, context.RefireCount < 5);
            }
        }

        public string ExpirationTime { get; set; }
        public string ResponseAddress { get; set; }
        public string FaultAddress { get; set; }
        public string Body { get; set; }
        public string MessageId { get; set; }
        public string ContentType { get; set; }
        public string RequestId { get; set; }
        public string CorrelationId { get; set; }
        public string ConversationId { get; set; }
        public string InitiatorId { get; set; }
        public string HeadersAsJson { get; set; }
        public string PayloadMessageHeadersAsJson { get; set; }

        Uri SerializedMessage.Destination => new Uri(Destination);

        private IPipe<SendContext> CreateMessageContext(Uri sourceAddress)
        {
            return new SerializedMessageContextAdapter(Pipe.Empty<SendContext>(), this, sourceAddress);
        }


        private class Scheduled
        {
        }
    }
}

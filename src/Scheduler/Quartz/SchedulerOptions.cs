namespace EverythingMessages.Scheduler.Quartz
{
    public class SchedulerOptions
    {
        public string RecurringTriggerPrefix { get; set; } = "Recurring.Trigger.";
        public uint PartitionCount { get; set; } = 1;
        public string PartitionGroup { get; set; } = "PARTITION_GROUP_ONE";
    }
}

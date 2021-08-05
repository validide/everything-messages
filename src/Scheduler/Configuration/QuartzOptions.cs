namespace EverythingMessages.Scheduler.Configuration
{
    public class QuartzOptions
    {
        public string ConnectionString { get; set; }
        public string Provider { get; set; }
        public int? ThreadCount { get; set; }
        public string InstanceName { get; set; }
        public string TablePrefix { get; set; }
        public bool? Clustered { get; set; }
        public string DriverDelegateType { get; set; }
        public uint PartitionCount { get; set; } = 1;
        /// <summary>
        /// Should the triggers be processed in batches?
        /// </summary>
        public bool EnableBatching { get; set; }
        /// <summary>
        /// The number of messages to process in the same batch.
        /// </summary>
        public int BatchSize { get; set; } = 10;
        /// <summary>
        /// The amount of time in milliseconds that a trigger is allowed to be acquired and fired ahead of its scheduled fire time.
        /// </summary>
        public int BatchHasten { get; set; } = 1000;
    }
}

namespace EverythingMessages.Scheduler;

public class SchedulerOptions
{
    public string InstanceName { get; set; }
    public string Provider { get; set; }
    public string DriverDelegateType { get; set; }
    public string ConnectionString { get; set; }
    public string TablePrefix { get; set; }
    public bool? Clustered { get; set; }
    public decimal ConcurrencyMultiplier { get; set; }
    public bool? EnableBatching { get; set; }
    public int BatchSize { get; set; }
    public int BatchHasten { get; set; }
}

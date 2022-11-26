namespace EverythingMessages.Infrastructure;

public class EndpointConfigurationOptions
{
    public string Name { get; set; }
    public string SchedulerQueue { get; set; }
    public int? ConcurrentMessageLimit { get; set; }
    public bool WaitBusStart { get; set; }
}

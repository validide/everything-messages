using System.Threading.Tasks;
using MassTransit;
using Quartz;

namespace EverythingMessages.Scheduler.Quartz
{
    public interface ISchedulerRepository
    {
        public string PartitionGroup { get; }
        public string InstanceId { get; }
        public uint PartitionCount { get; }
        public IScheduler GetScheduler(string triggerId);
        public Task StartAsync(IBus bus);
        public Task PauseAsync();
        public Task StopAsync();
    }
}

using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Scheduler.Quartz.Configuration
{
    /// <summary>
    /// Used to start and stop an in-memory scheduler using Quartz
    /// </summary>
    public class SchedulerBusObserver : IBusObserver
    {
        private readonly ISchedulerRepository _schedulerRepository;
        private readonly ILogger<SchedulerBusObserver> _logger;
        private readonly TaskCompletionSource _startScheduler;

        /// <summary>
        /// Creates the bus observer to initialize the Quartz scheduler.
        /// </summary>
        /// <param name="logger">Logger to log with.</param>
        public SchedulerBusObserver(ISchedulerRepository schedulerRepository, ILogger<SchedulerBusObserver> logger)
        {
            _schedulerRepository = schedulerRepository;
            _logger = logger;
            _startScheduler = new TaskCompletionSource();
        }

        public async Task<ISchedulerRepository> GetSchedulerRepository()
        {
            if (!_startScheduler.Task.IsCompleted)
            {
                await _startScheduler.Task.ConfigureAwait(false);
            }

            return _schedulerRepository;
        }

        public void PostCreate(IBus bus)
        {
        }

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStart(IBus bus)
        {
            return Task.CompletedTask;
        }

        public async Task PostStart(IBus bus, Task<BusReady> busReady)
        {
            _logger.LogDebug("Quartz Scheduler Starting: (PartitionGroup)", _schedulerRepository.PartitionGroup);

            await busReady.ConfigureAwait(false);

            //_schedulerRepository.JobFactory = new MassTransitJobFactory(bus, _schedulerRepository.JobFactory, _loggerFactory);

            try
            {
                await _schedulerRepository.StartAsync(bus).ConfigureAwait(false);

                _startScheduler.TrySetResult();
            }
            catch (Exception exception)
            {
                _startScheduler.TrySetException(exception);
                throw;
            }

            _logger.LogDebug("Quartz Scheduler Started: ({PartitionGroup}/{InstanceId})",
                _schedulerRepository.PartitionGroup,
                _schedulerRepository.InstanceId);
        }

        public Task StartFaulted(IBus bus, Exception exception)
        {
            return Task.CompletedTask;
        }

        public async Task PreStop(IBus bus)
        {
            await _schedulerRepository.PauseAsync().ConfigureAwait(false);

            _logger.LogDebug("Quartz Scheduler Paused: ({PartitionGroup}/{InstanceId})",
                _schedulerRepository.PartitionGroup,
                _schedulerRepository.InstanceId);
        }

        public async Task PostStop(IBus bus)
        {
            await _schedulerRepository.StopAsync().ConfigureAwait(false);

            _logger.LogDebug("Quartz Scheduler Stopped: ({PartitionGroup}/{InstanceId})",
                _schedulerRepository.PartitionGroup,
                _schedulerRepository.InstanceId);
        }

        public Task StopFaulted(IBus bus, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}

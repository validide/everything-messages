using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using EverythingMessages.Scheduler.Configuration;
using EverythingMessages.Scheduler.Quartz;
using GreenPipes.Partitioning;
using MassTransit;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace EverythingMessages.Scheduler
{
    public class ParitionedSchedulerRepository : ISchedulerRepository
    {
        private readonly SchedulerOptions _schedulerOptions;
        private readonly QuartzConfiguration _quartzConfiguration;
        private readonly IJobFactory _jobFactory;
        private readonly IHashGenerator _hashGenerator;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISchedulerFactory[] _schedulerFactories;
        private readonly IScheduler[] _schedulers;

        public ParitionedSchedulerRepository(
            SchedulerOptions schedulerOptions,
            QuartzConfiguration quartzConfiguration,
            IJobFactory jobFactory,
            IHashGenerator hashGenerator,
            ILoggerFactory loggerFactory
        )
        {
            _schedulerOptions = schedulerOptions;
            _quartzConfiguration = quartzConfiguration;
            _hashGenerator = hashGenerator;
            _jobFactory = jobFactory;
            _loggerFactory = loggerFactory;
            _schedulerFactories = new ISchedulerFactory[_schedulerOptions.PartitionCount];
            _schedulers = new IScheduler[_schedulerOptions.PartitionCount];
        }
        public string InstanceId => _schedulers[0]?.SchedulerInstanceId ?? String.Empty;

        public string PartitionGroup => _schedulerOptions.PartitionGroup;

        public uint PartitionCount => _schedulerOptions.PartitionCount;

        public IScheduler GetScheduler(string triggerId)
        {
            var hash = _hashGenerator.Hash(System.Text.Encoding.UTF8.GetBytes(triggerId));
            var idx = hash % _schedulers.Length;

            return _schedulers[idx];
        }
        public async Task PauseAsync()
        {
            foreach (var scheduler in _schedulers)
            {
                if (scheduler == null)
                    continue;

                await scheduler.Standby().ConfigureAwait(false);
            }
        }
        public async Task StartAsync(IBus bus)
        {
            for (int i = 0; i < _schedulers.Length; i++)
            {
                var cfg = _quartzConfiguration.Configuration;
                cfg["quartz.scheduler.instanceName"] += $"__{i:00000000.##}";
                var schedulerFactory = new StdSchedulerFactory(cfg);
                _schedulerFactories[i] = schedulerFactory;

                var scheduler = await schedulerFactory.GetScheduler().ConfigureAwait(false);
                scheduler.JobFactory = new MassTransitJobFactory(bus, _jobFactory, _loggerFactory);
                await scheduler.Start().ConfigureAwait(false);

                _schedulers[i] = scheduler;
            }
        }
        public async Task StopAsync()
        {
            foreach (var scheduler in _schedulers)
            {
                if (scheduler == null)
                    continue;

                await scheduler.Shutdown().ConfigureAwait(false);
            }
        }
    }
}

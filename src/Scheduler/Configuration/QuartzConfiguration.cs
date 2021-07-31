using System;
using System.Collections.Specialized;
using System.Text;
using EverythingMessages.Infrastructure;
using MassTransit.Transports.InMemory;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Scheduler.Configuration
{
    public class QuartzConfiguration
    {
        private readonly ILogger<QuartzConfiguration> _logger;
        private readonly QuartzOptions _options;
        private readonly EndpointConfigurationOptions _endpointOptions;

        public QuartzConfiguration(QuartzOptions options, EndpointConfigurationOptions endpointOptions, ILogger<QuartzConfiguration> logger)
        {
            _options = options;
            _logger = logger;
            _endpointOptions = endpointOptions;

            var queueName = endpointOptions.SchedulerQueue;
            if (String.IsNullOrWhiteSpace(queueName))
            {
                queueName = "quartz";
            }
            else if (Uri.IsWellFormedUriString(queueName, UriKind.Absolute))
            {
                queueName = new Uri(queueName).GetQueueOrExchangeName();
            }

            Queue = queueName;
        }

        public string Queue { get; }

        public int ConcurrentMessageLimit => _endpointOptions.ConcurrentMessageLimit ?? 16;
        private int MaxConcurrency => _options.ThreadCount ?? 10;

        public NameValueCollection Configuration
        {
            get
            {
                var configuration = new NameValueCollection()
                {
                    {"quartz.scheduler.instanceName", _options.InstanceName ?? "MassTransit-Scheduler"},
                    {"quartz.scheduler.instanceId", "AUTO"},
                    {"quartz.plugin.timeZoneConverter.type", "Quartz.Plugin.TimeZoneConverter.TimeZoneConverterPlugin, Quartz.Plugins.TimeZoneConverter"},
                    {"quartz.serializer.type", "json"},
                    {"quartz.threadPool.maxConcurrency", MaxConcurrency.ToString("F0")},
                    {"quartz.jobStore.misfireThreshold", "30000"},
                    {"quartz.jobStore.maxMisfiresToHandleAtATime", (MaxConcurrency / 2).ToString("F0")},
                    {"quartz.jobStore.driverDelegateType", _options.DriverDelegateType},
                    {"quartz.jobStore.tablePrefix", _options.TablePrefix ?? "QRTZ_"},
                    {"quartz.jobStore.clustered", $"{_options.Clustered ?? true}"},
                    {"quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz"},
                    {"quartz.jobStore.useProperties", "true"},
                    {"quartz.jobStore.dataSource", "default"},
                    {"quartz.dataSource.default.provider", _options.Provider},
                    {"quartz.dataSource.default.connectionString", _options.ConnectionString}
                };

                if (_options.EnableBatching)
                {
                    configuration.Add(new NameValueCollection()
                    {

                        {"quartz.jobStore.acquireTriggersWithinLock", "true"},
                        {"quartz.scheduler.batchTriggerAcquisitionMaxCount", _options.BatchSize.ToString("F0")},
                        {"quartz.scheduler.batchTriggerAcquisitionFireAheadTimeWindow", _options.BatchHasten.ToString("F0")}
                    });
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var information = new StringBuilder();
                    information.Append("Configuration values:");
                    foreach (var key in configuration.AllKeys)
                    {
                        information.AppendFormat("\n\t- {0}: {1}", key, configuration[key]);
                    }
                    _logger.LogInformation(information.ToString());
                }

                return configuration;
            }
        }
    }
}

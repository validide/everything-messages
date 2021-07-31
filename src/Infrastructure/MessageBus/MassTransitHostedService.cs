using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Infrastructure.MessageBus
{
    public class MassTransitHostedService : IHostedService
    {
        private readonly IBusControl _bus;
        private readonly ILogger _logger;
        private readonly EndpointConfigurationOptions _endpointConfigurationOptions;
        private Task _startTask;

        public MassTransitHostedService(EndpointConfigurationOptions endpointConfigurationOptions, IBusControl bus, ILoggerFactory loggerFactory)
        {
            _endpointConfigurationOptions = endpointConfigurationOptions;
            _bus = bus;
            _logger = loggerFactory.CreateLogger<MassTransitHostedService>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting bus");
            _startTask = _bus.StartAsync(cancellationToken);
            if(_endpointConfigurationOptions.WaitBusStart)
            {
                return _startTask;
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping bus");
            return _bus.StopAsync(cancellationToken);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public partial class AppStateController : ControllerBase
    {
        [LoggerMessage(0, LogLevel.Debug, "Called {action} from {controller}.")]
        private static partial void LogActionDetails(ILogger logger, string action, string controller);

        private readonly ILogger<AppStateController> _logger;

        public AppStateController(ILogger<AppStateController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            LogActionDetails(_logger, nameof(AppStateController.Get), nameof(AppStateController));
            return $"{HttpStatusCode.OK:D} - {HttpStatusCode.OK:G}";
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AppStateController : ControllerBase
    {
        private readonly ILogger<AppStateController> _logger;

        public AppStateController(ILogger<AppStateController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            _logger.LogDebug("Called {action} from {controller}.", nameof(AppStateController.Get), nameof(AppStateController));
            return $"{HttpStatusCode.OK:D} - {HttpStatusCode.OK:G}";
        }
    }
}

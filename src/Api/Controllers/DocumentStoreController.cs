using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EverythingMessages.Api.Infrastructure.DocumentStore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public partial class DocumentStoreController : ControllerBase
    {
        [LoggerMessage(0, LogLevel.Information, "Created document {id}")]
        private static partial void LogDocumentCreaton(ILogger logger, string id);

        private static readonly Random s_random = new();
        private readonly ILogger<DocumentStoreController> _logger;
        private readonly IDocumentStore _store;

        public DocumentStoreController(IDocumentStore store, ILogger<DocumentStoreController> logger)
        {
            _store = store;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var ids = await _store.ListAsync(CancellationToken.None).ConfigureAwait(false);
            return new JsonResult(ids);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var item = await _store.GetAsync(id, CancellationToken.None).ConfigureAwait(false);
            return new JsonResult(
                new { id = id, data = item }
            );
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            var length = s_random.Next(1_000, 10_000);
            var data = new byte[length];
            s_random.NextBytes(data);

            var id = await _store.StoreAsync(data, CancellationToken.None).ConfigureAwait(false);
            LogDocumentCreaton(_logger, id);
            return Accepted(Url.Action("GET", "DocumentStore", new { id }));
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string id)
        {
            await _store.RemoveAsync(id, CancellationToken.None).ConfigureAwait(false);
            return StatusCode((int)HttpStatusCode.NoContent);
        }
    }
}

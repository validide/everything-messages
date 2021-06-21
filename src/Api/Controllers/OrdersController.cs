using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using EverythingMessages.Api.Infrastructure.DocumentStore;
using EverythingMessages.Components.Orders;
using EverythingMessages.Contracts.Orders;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace EverythingMessages.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrdersController : ControllerBase
    {
        private static readonly Random s_random = new();
        private readonly ILogger<DocumentStoreController> _logger;
        private readonly IDocumentStore _store;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IEndpointNameFormatter _nameFormatter;
        readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;

        public OrdersController(
            IDocumentStore store,
            ISendEndpointProvider sendEndpointProvider,
            IEndpointNameFormatter nameFormatter,
            IRequestClient<SubmitOrder> submitOrderRequestClient,
            ILogger<DocumentStoreController> logger
        )
        {
            _store = store;
            _sendEndpointProvider = sendEndpointProvider;
            _nameFormatter = nameFormatter;
            _submitOrderRequestClient = submitOrderRequestClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post(bool? sync, string customer)
        {
            var length = s_random.Next(1_000, 10_000);
            var data = new byte[length];
            s_random.NextBytes(data);

            var id = await _store.StoreAsync(data, CancellationToken.None).ConfigureAwait(false);
            var order = new SubmitOrder { Id = id, CustomerId = customer };
            if (sync ?? false)
            {
                var response = await _submitOrderRequestClient
                    .GetResponse<OrderSubmissionAccepted, OrderSubmissionRejected>(order)
                    .ConfigureAwait(false);

                if (response.Is(out Response<OrderSubmissionAccepted> accepted))
                {
                    return Ok(accepted.Message);
                }
                else if (response.Is(out Response<OrderSubmissionRejected> rejected))
                {
                    return BadRequest(rejected.Message);
                }
            }
            else
            {
                var endpoint = await _sendEndpointProvider
                .GetSendEndpoint(new Uri($"queue:{_nameFormatter.Consumer<SubmitOrderConsumer>()}"))
                .ConfigureAwait(false);

                await endpoint.Send(order).ConfigureAwait(false);
                _logger.LogInformation("Created order {id}", id);
                return Accepted(Url.Action("GET", "DocumentStore", new { id }));
            }

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}

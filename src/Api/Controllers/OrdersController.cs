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
    public partial class OrdersController : ControllerBase
    {
        [LoggerMessage(0, LogLevel.Information, "Created order {id}")]
        private static partial void LogOrderCreaton(ILogger logger, string id);

        private static readonly Random s_random = new();
        private readonly ILogger<DocumentStoreController> _logger;
        private readonly IDocumentStore _store;
        private readonly ISendEndpointProvider _sendEndpointProvider;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IEndpointNameFormatter _nameFormatter;
        readonly IRequestClient<SubmitOrder> _submitOrderRequestClient;

        public OrdersController(
            IDocumentStore store,
            ISendEndpointProvider sendEndpointProvider,
            IPublishEndpoint publishEndpoint,
            IEndpointNameFormatter nameFormatter,
            IRequestClient<SubmitOrder> submitOrderRequestClient,
            ILogger<DocumentStoreController> logger
        )
        {
            _store = store;
            _sendEndpointProvider = sendEndpointProvider;
            _publishEndpoint = publishEndpoint;
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
            switch (sync)
            {
                case true:
                    {
                        /*
                         * We want to wait for the response from the order processor.
                         * Depnding on how the request client is register it will either:
                         *  - send directly to the queue in which case the auditor will not get a copy of the message:
                         *    mt.AddRequestClient<SubmitOrder>(new Uri($"queue:{nameFormatter.Consumer<SubmitOrderConsumer>()}"))
                         *  - send to the exchange which will forward to all the registered queues:
                         *    mt.AddRequestClient<SubmitOrder>()
                         *    
                         * !!! WARNING !!! 
                         *  If you publish to the exchange and the consumer queue is not created and registered with the exchange
                         *  the message will be LOST!
                         */
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
                        break;
                    }
                case false:
                    {
                        /*
                         * We do not want to wait for a response from the order processor.
                         * !!! WARNING !!! 
                         *  Since we are publishing to the exchange if the consumer queue is not created and registered with
                         *  the exchange the message will be LOST!
                         */
                        await _publishEndpoint.Publish(order).ConfigureAwait(false);
                        LogOrderCreaton(_logger, id);
                        return Accepted(Url.Action("GET", "DocumentStore", new { id }));
                    }
                default:
                    {
                        /*
                         * We do not want to wait for a response from the order processor.
                         * By sending directly to the queue we do not need to worry about the queue. If the queue
                         * doesnot exist it will be created and the messages will be preserved there.
                         */

                        var queue = $"queue:{_nameFormatter.Consumer<SubmitOrderConsumer>()}__missing";
                        if ("MISSING_QUEUE".Equals(customer, StringComparison.InvariantCultureIgnoreCase))
                        {
                            queue += "_missing_consumer";
                        }
                        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri(queue)).ConfigureAwait(false);

                        await endpoint.Send(order).ConfigureAwait(false);
                        LogOrderCreaton(_logger, id);
                        return Accepted(Url.Action("GET", "DocumentStore", new { id }));
                    }
            }

            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}

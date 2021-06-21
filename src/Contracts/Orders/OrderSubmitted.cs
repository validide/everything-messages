using System;

namespace EverythingMessages.Contracts.Orders
{
    public class OrderSubmitted
    {
        public string Id { get; set; }
        public string CustomerId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

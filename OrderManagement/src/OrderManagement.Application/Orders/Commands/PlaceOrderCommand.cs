using MediatR;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Orders.Commands
{
    public class PlaceOrderCommand : IRequest<Guid>
    {
        public Guid CustomerId { get; set; }
        public Address ShippingAddress { get; set; } = null!;
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}

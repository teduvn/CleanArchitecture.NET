using MediatR;
using OrderManagement.Application.Orders.DTOs;
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
        public AddressDto ShippingAddress { get; set; } = null!;
        public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
    }
}

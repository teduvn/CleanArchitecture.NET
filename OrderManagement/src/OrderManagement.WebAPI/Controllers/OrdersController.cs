using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.Orders.Commands;
using OrderManagement.Application.Orders.Commands.PlaceOrder;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Queries;

namespace OrderManagement.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // POST /api/orders
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            var shippingAddress = new AddressDto()
            {
                Street = request.ShippingAddress.Street,
                City = request.ShippingAddress.City,
                Province = request.ShippingAddress.Province,
                Country = request.ShippingAddress.Country,
                PostalCode = request.ShippingAddress.PostalCode
            };
            var command = new PlaceOrderCommand
            {
                CustomerId = request.CustomerId,
                ShippingAddress = shippingAddress,
                Items = request.Items.Select(i => new OrderItemDto
                                                        {
                                                            ProductId = i.ProductId,
                                                            Quantity = i.Quantity
                                                        }).ToList()
                                                };

            var orderId = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetOrder), new { id = orderId }, new { orderId });
        }

        // GET /api/orders/{id}
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            var query = new GetOrderByIdQuery(id);
            var order = await _mediator.Send(query);

            return order is null ? NotFound() : Ok(order);
        }
    }

}

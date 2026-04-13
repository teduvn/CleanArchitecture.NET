using MediatR;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Mappings;
using OrderManagement.Domain.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderManagement.Application.Orders.Queries
{
    /// <summary>
    /// Query lấy chi tiết một Order theo Id.
    /// </summary>
    public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

    /// <summary>
    /// Handler xử lý GetOrderByIdQuery.
    /// Demo cách sử dụng ToDto() extension method.
    /// </summary>
    public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
        : IRequestHandler<GetOrderByIdQuery, OrderDto?>
    {
        public async Task<OrderDto?> Handle(
            GetOrderByIdQuery request,
            CancellationToken ct)
        {
            var order = await orderRepository.GetByIdAsync(request.OrderId, ct);

            // Sử dụng extension method ToDto()
            return order?.ToDto();
        }
    }
}

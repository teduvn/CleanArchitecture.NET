using MediatR;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Mappings;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.Specifications.Orders;

namespace OrderManagement.Application.Orders.Queries
{
    public class GetActiveOrdersByCustomerQuery : IRequest<IReadOnlyList<OrderDto>>
    {
        public Guid CustomerId { get; init; }
    }

    public class GetActiveOrdersByCustomerQueryHandler(
        IOrderRepository orderRepository)
        : IRequestHandler<GetActiveOrdersByCustomerQuery, IReadOnlyList<OrderDto>>
    {
        public async Task<IReadOnlyList<OrderDto>> Handle(
            GetActiveOrdersByCustomerQuery request,
            CancellationToken ct)
        {
            // Specification được khởi tạo với tham số cụ thể
            var spec = new ActiveOrdersByCustomerSpecification(request.CustomerId);

            // Repository không biết gì về logic filter — chỉ áp Specification
            var orders = await orderRepository.ListAsync(spec, ct);

            // Map sang DTO (bài 3 sẽ đi sâu vào phần này)
            return orders.Select(o => o.ToDto()).ToList().AsReadOnly();
        }
    }

}

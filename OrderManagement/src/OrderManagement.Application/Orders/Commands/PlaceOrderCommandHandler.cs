using MediatR;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Orders.Commands
{
    public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Guid>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PlaceOrderCommandHandler(
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> Handle(
            PlaceOrderCommand command,
            CancellationToken cancellationToken)
        {
            var address = new Address(command.ShippingAddress.Street, command.ShippingAddress.City, command.ShippingAddress.Province, command.ShippingAddress.Country, command.ShippingAddress.PostalCode);
            // 1. Tạo order từ domain logic
            var order = Order.Create(command.CustomerId, address, new List<OrderItem>());

            foreach (var item in command.Items)
            {
                var unitPrice = Money.Create(item.UnitPrice, item.Currency);
                order.AddItem(item.ProductId, item.ProductName, unitPrice, item.Quantity);
            }    

            // 2. Đăng ký vào repository (chưa commit)
            _orderRepository.Add(order);

            // 3. Commit toàn bộ thay đổi — một lần duy nhất
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return order.Id;
        }
    }

}

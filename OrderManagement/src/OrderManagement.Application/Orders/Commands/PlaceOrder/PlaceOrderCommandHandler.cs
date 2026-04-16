using MediatR;
using OrderManagement.Application.Common;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Orders;
using OrderManagement.Domain.Repositories;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Application.Orders.Commands.PlaceOrder
{
    public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, Result<Guid>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PlaceOrderCommandHandler(
            IOrderRepository orderRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(
            PlaceOrderCommand command,
            CancellationToken cancellationToken)
        {
            var address = new Address(command.ShippingAddress.Street, command.ShippingAddress.City, command.ShippingAddress.Province, command.ShippingAddress.Country, command.ShippingAddress.PostalCode);

            // Step 1: Validate business preconditions
            // — dùng Result.Failure thay vì throw
            var customer = await _customerRepository
                .GetByIdAsync(command.CustomerId, cancellationToken);

            if (customer is null)
                return OrderErrors.CustomerNotFound(command.CustomerId);
            // implicit conversion: Error → Result<Guid>

            if (command.Items == null || command.Items.Count == 0)
                return OrderErrors.EmptyItems;

            // Step 2: Delegate sang Domain để tạo Aggregate
            // Domain sẽ enforce invariant, throw DomainException nếu vi phạm
            var orderResult = Order.CreateDraft(command.CustomerId, address);

            if (orderResult.IsFailure)
                return orderResult.Error;

            var order = orderResult.Value;

            foreach (var itemDto in command.Items)
            {
                var money = new Money(itemDto.UnitPrice, itemDto.Currency);
                order.AddItem(itemDto.ProductId, itemDto.ProductName, money, itemDto.Quantity);
            }

            // Step 3: Persist
            _orderRepository.Add(order);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Step 4: Return success với data
            return Result<Guid>.Success(order.Id);

        }
    }
}

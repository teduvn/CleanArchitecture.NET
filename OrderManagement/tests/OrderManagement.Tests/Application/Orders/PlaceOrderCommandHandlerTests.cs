using FluentAssertions;
using NSubstitute;
using OrderManagement.Application.Orders.Commands;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Interfaces;
using OrderManagement.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Tests.Application.Orders
{
    public class PlaceOrderCommandHandlerTests
    {
        private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
        private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
        private readonly PlaceOrderCommandHandler _handler;

        public PlaceOrderCommandHandlerTests()
        {
            _handler = new PlaceOrderCommandHandler(_repo, _uow);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesOrderAndReturnsId()
        {
            // Arrange
            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        ProductId = Guid.NewGuid(),
                        ProductName = "Test Product",
                        UnitPrice = 150_000m,
                        Currency = "VND",
                        Quantity = 2
                    }
                }
            };

            // Act
            var orderId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            orderId.Should().NotBeEmpty();
            // Verify repository được gọi đúng 1 lần
            _repo.Received(1).Add(Arg.Any<Order>());
            await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_EmptyItems_ThrowsDomainException()
        {
            // Arrange
            var command = new PlaceOrderCommand
            {
                CustomerId = Guid.NewGuid(),
                ShippingAddress = new AddressDto
                {
                    Street = "123 Main St",
                    City = "Hanoi",
                    Province = "Hanoi",
                    Country = "Vietnam",
                    PostalCode = "100000"
                },
                Items = new List<OrderItemDto>() // Empty!
            };

            // Act
            var act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert — DomainException được throw từ Order.Create()
            await act.Should().ThrowAsync<DomainException>()
                .WithMessage("*ít nhất một sản phẩm*");
        }

    }
}

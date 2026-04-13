using FluentAssertions;
using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Events;
using OrderManagement.Domain.ValueObjects;

namespace OrderManagement.Tests.Unit.Domain
{
    public class OrderTests
    {
        [Fact]
        public void AddItem_ValidProduct_ShouldIncreaseTotalAmount()
        {
            // Arrange
            var order = Order.Create(
                Guid.NewGuid(),
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"),
                new List<OrderItem>());
            var price = Money.Create(100_000, "VND");

            // Act
            order.AddItem(Guid.NewGuid(), "Laptop", price, 2);

            // Assert
            order.TotalAmount.Amount.Should().Be(200_000);
            order.Items.Should().ContainSingle();
        }


        [Fact]
        public void AddItem_WithNegativeQuantity_ShouldThrowDomainException()
        {
            // Arrange
            var order = Order.Create(
                Guid.NewGuid(),
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"),
                new List<OrderItem>());

            // Act
            var act = () => order.AddItem(Guid.NewGuid(), "Laptop", Money.Create(100_000, "VND"), -1);

            // Assert — Quantity âm phải throw - invariant được enforce
            act.Should().Throw<DomainException>()
                .WithMessage("*lớn hơn 0*");
        }


        [Fact]
        public void Place_WithNoItems_ShouldThrowDomainException()
        {
            // Arrange
            var order = Order.Create(
                Guid.NewGuid(),
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"),
                new List<OrderItem>());

            // Act
            var act = () => order.Place();

            // Assert — Order rỗng không được đặt hàng
            act.Should().Throw<DomainException>()
                .WithMessage("*ít nhất một sản phẩm*");
        }

        [Fact]
        public void Create_ShouldRaiseOrderPlacedEvent()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var shippingAddress = new Address("123 Main St", "Hanoi", "Vietnam", "100000");
            var productId = Guid.NewGuid();

            // Act
            var order = Order.Create(customerId, shippingAddress, new List<OrderItem>());
            order.AddItem(productId, "Product A", Money.Create(100_000, "VND"), 2);

            // Assert — không cần database, không cần DI
            var domainEvent = order.DomainEvents.Should().ContainSingle()
                .Which.Should().BeOfType<OrderPlacedEvent>().Subject;

            domainEvent.CustomerId.Should().Be(customerId);
            domainEvent.TotalAmount.Should().Be(0); // Event raised on Create, before items added
            domainEvent.Items.Should().BeEmpty();
        }

        [Fact]
        public void Ship_WhenOrderNotConfirmed_ShouldThrowDomainException()
        {
            // Arrange — Order mới tạo có status Draft
            var customerId = Guid.NewGuid();
            var shippingAddress = new Address("123 Main St", "Hanoi", "Vietnam", "100000");
            var order = Order.Create(customerId, shippingAddress, new List<OrderItem>());
            order.AddItem(Guid.NewGuid(), "Product A", Money.Create(100_000, "VND"), 1);

            // Act
            var act = () => order.Ship("TRACK123", DateTime.UtcNow.AddDays(3));

            // Assert
            act.Should().Throw<DomainException>()
                .WithMessage("*confirmed order*");
        }

    }
}

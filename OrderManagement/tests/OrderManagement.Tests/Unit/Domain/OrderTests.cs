using OrderManagement.Domain.Common;
using OrderManagement.Domain.Entities;
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
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"));
            var price = new Money(100_000, "VND");


            // Act
            order.AddItem(Guid.NewGuid(), "Laptop", price, 2);


            // Assert
            Assert.Equal(200_000, order.TotalAmount.Amount);
            Assert.Single(order.Items);
        }


        [Fact]
        public void AddItem_WithNegativeQuantity_ShouldThrowDomainException()
        {
            var order = Order.Create(Guid.NewGuid(),
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"));


            // Quantity âm phải throw - invariant được enforce
            Assert.Throws<DomainException>(() =>
                order.AddItem(Guid.NewGuid(), "Laptop", new Money(100_000, "VND"), -1));
        }


        [Fact]
        public void PlaceOrder_WithNoItems_ShouldThrowDomainException()
        {
            var order = Order.Create(Guid.NewGuid(),
                new Address("123 Main St", "Hanoi", "Vietnam", "100000"));


            // Order rỗng không được đặt hàng
            Assert.Throws<DomainException>(() => order.Place());
        }
    }
}

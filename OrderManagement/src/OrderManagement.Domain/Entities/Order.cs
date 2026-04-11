using OrderManagement.Domain.Common;
using OrderManagement.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public sealed class Order : Entity<Guid>
    {
        private readonly List<OrderItem> _items = new();

        // Private setters — chỉ thay đổi qua method, không assign trực tiếp từ ngoài
        public Guid CustomerId { get; private set; }
        public Address ShippingAddress { get; private set; } = null!;
        public OrderStatus Status { get; private set; }
        public Money TotalAmount { get; private set; } = null!;
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }

        // Read-only collection — caller không thể modify list trực tiếp
        public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

        // EF Core cần constructor không tham số (private để không expose)
        private Order() { }

        // Factory method thay vì constructor public — kiểm soát invariant
        public static Order Create(Guid customerId, Address shippingAddress)
        {
            ArgumentException.ThrowIfNullOrEmpty(customerId.ToString());
            if (customerId == Guid.Empty)
                throw new DomainException("CustomerId không hợp lệ");
            if (shippingAddress is null)
                throw new DomainException("Địa chỉ giao hàng không được để trống");

            //order.RaiseDomainEvent(new OrderCreatedEvent(order.Id));

            return new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ShippingAddress = shippingAddress,
                Status = OrderStatus.Draft,
                TotalAmount = Money.Zero("VND"),  // default VND, sẽ recalculate
                CreatedAt = DateTime.UtcNow
            };
        }

        // Business method — thêm item qua Order, không tạo OrderItem trực tiếp
        public void AddItem(Guid productId, string productName, Money unitPrice, int quantity)
        {
            EnsureOrderIsModifiable();

            if (Status != OrderStatus.Draft)
                throw new DomainException("Chỉ có thể thêm item vào đơn hàng ở trạng thái Draft.");

            if (quantity <= 0)
                throw new DomainException("Số lượng phải lớn hơn 0.");

            // Nếu đã có product này, tăng quantity thay vì thêm dòng mới
            var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.IncreaseQuantity(quantity);
            }
            else
            {
                var item = OrderItem.Create(Id, productId, productName, unitPrice, quantity);
                _items.Add(item);
            }

            RecalculateTotal();
        }

        public void RemoveItem(Guid orderItemId)
        {
            EnsureOrderIsModifiable();

            if (Status != OrderStatus.Draft)
                throw new DomainException("Không thể xoá item khỏi đơn đã được xử lý.");

            var item = _items.FirstOrDefault(i => i.Id == orderItemId)
                ?? throw new NotFoundException($"Không tìm thấy item {orderItemId}.");

            _items.Remove(item);
            RecalculateTotal();
        }

        public void Place()
        {
            if (Status != OrderStatus.Draft)
                throw new DomainException("Chỉ có thể đặt đơn hàng ở trạng thái Draft.");

            if (!_items.Any())
                throw new DomainException("Đơn hàng phải có ít nhất một sản phẩm.");

            Status = OrderStatus.Placed;
            UpdatedAt = DateTime.UtcNow;
            // Domain event sẽ được cover ở bài 2.3
            //RaiseDomainEvent(new OrderPlacedEvent(Id, CustomerId, TotalAmount));

        }

        private void RecalculateTotal()
        {
            TotalAmount = _items.Aggregate(
                Money.Zero("VND"),
                (sum, item) => sum.Add(item.Subtotal));
        }

        // ======================================================
        // PRIVATE HELPER - enforce invariant tập trung
        // ======================================================
        private void EnsureOrderIsModifiable()
        {
            if (Status is OrderStatus.Shipped or OrderStatus.Cancelled)
                throw new DomainException(
                    $"Không thể chỉnh sửa Order ở trạng thái {Status}");
        }

    }

    public enum OrderStatus
    {
        Draft = 0,
        Placed = 1,
        Confirmed = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }

}

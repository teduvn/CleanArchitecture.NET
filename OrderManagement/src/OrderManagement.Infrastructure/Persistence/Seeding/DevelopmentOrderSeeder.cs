using OrderManagement.Domain.ValueObjects;
using OrderManagement.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed đơn hàng giả để dev và test. KHÔNG dùng trong production.
    /// </summary>
    public class DevelopmentOrderSeeder(ApplicationDbContext context) : IDataSeeder
    {
        public int Order => 10; // Chạy sau — cần Customer và Product có sẵn

        public async Task SeedAsync(CancellationToken ct = default)
        {
            if (await context.Orders.AnyAsync(ct))
                return;

            // Lấy customer đầu tiên đã được seed
            var customerId = await context.Customers
                .Select(c => c.Id)
                .FirstOrDefaultAsync(ct);

            if (customerId == Guid.Empty)
                return; // Không có customer, skip seeding

            // Seed một vài products nếu chưa có
            if (!await context.Products.AnyAsync(ct))
            {
                var products = Enumerable.Range(1, 10).Select(i =>
                    Product.Create(
                        name: $"Sample Product {i}",
                        description: $"Description for product {i}",
                        price: Money.Create(Random.Shared.Next(100, 1000) * 1000m, "VND"),
                        weightKg: Random.Shared.Next(1, 20) * 0.5m,
                        stockQuantity: Random.Shared.Next(50, 200)
                    )
                ).ToList();

                await context.Products.AddRangeAsync(products, ct);
                await context.SaveChangesAsync(ct);
            }

            // Lấy danh sách products
            var productList = await context.Products.Take(10).ToListAsync(ct);
            if (!productList.Any())
                return;

            // Tạo shipping address mẫu
            var addresses = new[]
            {
                Address.Create("123 Nguyen Hue", "Ho Chi Minh", "HCM", "VN", "70000"),
                Address.Create("456 Le Loi", "Ha Noi", "HN", "VN", "10000"),
                Address.Create("789 Tran Hung Dao", "Da Nang", "DN", "VN", "50000")
            };

            var orderList = new List<Domain.Entities.Order>();
            for (int i = 1; i <= 20; i++)
            {
                var address = addresses[Random.Shared.Next(addresses.Length)];
                var orderResult = Domain.Entities.Order.CreateDraft(customerId, address);

                if (!orderResult.IsSuccess)
                    continue;

                var order = orderResult.Value;

                // Thêm 1-3 items ngẫu nhiên
                var itemCount = Random.Shared.Next(1, 4);
                for (int j = 0; j < itemCount; j++)
                {
                    var product = productList[Random.Shared.Next(productList.Count)];
                    order.AddItem(
                        productId: product.Id,
                        productName: product.Name,
                        unitPrice: product.Price,
                        quantity: Random.Shared.Next(1, 5)
                    );
                }

                orderList.Add(order);
            }

            await context.Orders.AddRangeAsync(orderList, ct);
            await context.SaveChangesAsync(ct);
        }
    }

}

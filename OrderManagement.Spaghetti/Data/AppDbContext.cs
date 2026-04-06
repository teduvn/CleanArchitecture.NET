using Microsoft.EntityFrameworkCore;
using OrderManagement.Spaghetti.Models;

namespace OrderManagement.Spaghetti.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tất cả config nhét thẳng vào đây — một file to dần theo thời gian
        modelBuilder.Entity<Order>(e =>
        {
            e.Property(x => x.Total).HasPrecision(18, 2);
            e.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            e.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DiscountCode>(e =>
        {
            e.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            e.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
        });

        // Seed data
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Dell XPS 13", Price = 25_000_000, Stock = 10, Category = "Electronics", Description = "Laptop cao cấp" },
            new Product { Id = 2, Name = "Chuột Logitech MX Master", Price = 2_500_000, Stock = 50, Category = "Accessories", Description = "Chuột không dây" },
            new Product { Id = 3, Name = "Bàn phím cơ Keychron K2", Price = 3_200_000, Stock = 30, Category = "Accessories", Description = "Bàn phím cơ không dây" },
            new Product { Id = 4, Name = "Màn hình LG 27 inch 4K", Price = 12_000_000, Stock = 15, Category = "Electronics", Description = "Màn hình 4K IPS" },
            new Product { Id = 5, Name = "Tai nghe Sony WH-1000XM5", Price = 8_500_000, Stock = 25, Category = "Electronics", Description = "Tai nghe chống ồn" }
        );

        modelBuilder.Entity<DiscountCode>().HasData(
            new DiscountCode { Id = 1, Code = "TEDU10", DiscountPercent = 10, MinOrderAmount = 1_000_000, ExpiresAt = DateTime.UtcNow.AddDays(30), IsActive = true },
            new DiscountCode { Id = 2, Code = "SALE20", DiscountPercent = 20, MinOrderAmount = 5_000_000, ExpiresAt = DateTime.UtcNow.AddDays(7), IsActive = true }
        );
    }
}

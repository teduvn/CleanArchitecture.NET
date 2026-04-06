using Microsoft.EntityFrameworkCore;
using OrderManagement.Spaghetti.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Order Management API (Spaghetti Version)",
        Version = "v1",
        Description = """
            ⚠️ Demo project cho khóa học Clean Architecture với .NET
            
            Đây là phiên bản TRƯỚC khi refactor — cố tình vi phạm các nguyên tắc 
            kiến trúc để minh họa vấn đề thực tế.
            
            Các vấn đề được đánh dấu trong code comment với prefix "BUG:" và "VẤN ĐỀ #N".
            """
    });
});

// Dùng InMemory database để chạy không cần SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseInMemoryDatabase("OrderManagementDb");
        // BẬT query logging để demo N+1 problem trong bài giảng
        options.EnableSensitiveDataLogging();
        options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

var app = builder.Build();

// Seed data khi dùng InMemory database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Thêm seed data thủ công vì InMemory không chạy HasData()
    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new() { Id = 1, Name = "Laptop Dell XPS 13",        Price = 25_000_000, Stock = 10, Category = "Electronics",  IsActive = true },
            new() { Id = 2, Name = "Chuột Logitech MX Master",  Price =  2_500_000, Stock = 50, Category = "Accessories",  IsActive = true },
            new() { Id = 3, Name = "Bàn phím cơ Keychron K2",   Price =  3_200_000, Stock = 30, Category = "Accessories",  IsActive = true },
            new() { Id = 4, Name = "Màn hình LG 27 inch 4K",    Price = 12_000_000, Stock = 15, Category = "Electronics",  IsActive = true },
            new() { Id = 5, Name = "Tai nghe Sony WH-1000XM5",  Price =  8_500_000, Stock = 25, Category = "Electronics",  IsActive = true }
        );

        db.DiscountCodes.AddRange(
            new() { Id = 1, Code = "TEDU10", DiscountPercent = 10, MinOrderAmount = 1_000_000, ExpiresAt = DateTime.UtcNow.AddDays(30), IsActive = true, MaxUsage = 100 },
            new() { Id = 2, Code = "SALE20", DiscountPercent = 20, MinOrderAmount = 5_000_000, ExpiresAt = DateTime.UtcNow.AddDays(7),  IsActive = true, MaxUsage = 50  }
        );

        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Management API v1");
        c.RoutePrefix = string.Empty; // Swagger UI tại root URL
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

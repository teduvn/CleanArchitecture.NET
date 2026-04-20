using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Infrastructure.Persistence.Seeding
{
    /// <summary>
    /// Seed dữ liệu Role vào database.
    /// Idempotent: kiểm tra trước khi insert, không tạo duplicate.
    /// </summary>
    public class RoleSeedDataSeeder(ApplicationDbContext context) : IDataSeeder
    {
        public int Order => 1; // Chạy trước — Role cần có trước Permission

        public async Task SeedAsync(CancellationToken ct = default)
        {
            //// Chỉ seed nếu bảng rỗng — kiểm tra AnyAsync thay vì Count
            //if (await context.Roles.AnyAsync(ct))
            //    return; // Đã có data, không làm gì thêm

            //var roles = new[]
            //{
            //    new Role { Id = Guid.Parse("..."), Name = "Admin", NormalizedName = "ADMIN" },
            //    new Role { Id = Guid.Parse("..."), Name = "Manager", NormalizedName = "MANAGER" },
            //    new Role { Id = Guid.Parse("..."), Name = "Staff", NormalizedName = "STAFF" }
            //};

            //await context.Roles.AddRangeAsync(roles, ct);
            //await context.SaveChangesAsync(ct);
        }
    }

}

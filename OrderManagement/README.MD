# Tạo migration mới
# --project: project chứa DbContext và Migration
# --startup-project: project có appsettings (thường là WebAPI)
dotnet ef migrations add InitialCreate --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI --output-dir Persistence/Migrations

# Xem danh sách migration đã tạo
dotnet ef migrations list --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI

# Apply migration lên database (CHỈ dùng ở dev/test)
dotnet ef database update --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI

# Xóa migration cuối (chưa apply) khi cần sửa
dotnet ef migrations remove --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI

# Bước 1: Export SQL script từ migration
# --idempotent: script kiểm tra trước khi chạy, không apply migration đã có
# --output: lưu ra file để review
dotnet ef migrations script --idempotent --output src/OrderManagement.Infrastructure/Persistence/Scripts/migration_20042026.sql --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI

# Bước 2: Review SQL file — đọc từng câu lệnh
# - Có DROP TABLE/COLUMN nào không?
# - Index mới có ảnh hưởng performance production không?
# - Data migration (UPDATE) có đúng logic không?

# Bước 3: Apply trên staging trước
sqlcmd -S staging-db -U dbuser -P pass -d OrderManagement -i migration_20250115.sql

# Bước 4: Verify staging hoạt động bình thường
# Bước 5: Apply lên production trong maintenance window
sqlcmd -S prod-db -U dbuser -P pass -d OrderManagement -i migration_20250115.sql


dotnet user-secrets set "SendGrid:ApiKey" "SG.xxxxx"

dotnet ef migrations add AddIdentityTables --project src/OrderManagement.Infrastructure --startup-project src/OrderManagement.WebAPI --output-dir Persistence/Migrations
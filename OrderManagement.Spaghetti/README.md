# OrderManagement.Spaghetti

> ⚠️ **Demo project cho Bài 01 — khóa học Clean Architecture với .NET**  
> Code này cố tình vi phạm các nguyên tắc kiến trúc để minh họa vấn đề thực tế.  
> **KHÔNG dùng pattern này trong production.**

---

## Chạy nhanh

```bash
cd OrderManagement.Spaghetti
dotnet run
```

Mở trình duyệt: `http://localhost:5000` → Swagger UI tự động mở.

Dùng **InMemory database** — không cần cài SQL Server, không cần migration.

---

## Hướng dẫn demo cho giảng viên

### Bước 1 — Bật EF Core Query Logging

`Program.cs` đã bật sẵn:
```csharp
options.LogTo(Console.WriteLine, LogLevel.Information);
options.EnableSensitiveDataLogging();
```

Khi gọi `POST /api/orders`, console sẽ hiện từng SQL query — học viên sẽ thấy N+1 trực tiếp.

---

### Bước 2 — Demo N+1 Problem

Gọi `POST /api/orders` với đơn hàng có 3 sản phẩm:

```json
{
  "customerEmail": "demo@tedu.com.vn",
  "customerName": "Nguyễn Văn A",
  "shippingAddress": "123 Nguyễn Huệ, Q1, TP.HCM",
  "items": [
    { "productId": 1, "quantity": 1 },
    { "productId": 2, "quantity": 2 },
    { "productId": 3, "quantity": 1 }
  ]
}
```

**Kết quả quan sát được trong console:**  
- 3 query `FindAsync(productId)` cho vòng lặp validate  
- 3 query `FindAsync(productId)` nữa cho vòng lặp tạo OrderItem  
→ **6 query cho 3 sản phẩm**. Đơn hàng 10 sản phẩm = 20 query.

---

### Bước 3 — Demo "Không test được"

Thử viết unit test cho discount logic:

```csharp
// Test này KHÔNG thể viết được vì discount logic nằm trong controller
// Muốn test phải có: AppDbContext + database + HTTP request
[Fact]
public void Order_Over_1Million_Should_Get_10_Percent_Discount()
{
    // Để test dòng này trong controller:
    // if (subtotal >= 1_000_000) discountAmount = subtotal * 0.10m;
    
    // Bắt buộc phải tạo AppDbContext...
    // Bắt buộc phải có database đang chạy...
    // Bắt buộc phải gọi HTTP request...
    // Không thể test đơn giản như thế này:
    
    // var discount = DiscountCalculator.Calculate(1_500_000); // Class này không tồn tại
    // Assert.Equal(150_000, discount);
}
```

---

### Bước 4 — Demo Race Condition với Discount Code

Giải thích đoạn code này:
```csharp
// BUG RACE CONDITION: Đọc count rồi mới tăng
// Nếu 2 request đến cùng lúc cùng dùng 1 mã → cả 2 đều pass
discount.UsageCount++;
```

Dùng mã `TEDU10` với 2 request gần như đồng thời để minh họa.

---

### Bước 5 — Demo DateTime.Now vs UtcNow

Đếm số lần `DateTime.Now` xuất hiện trong `OrdersController.cs`:
- Dòng 104: `CreatedAt = DateTime.Now`  
- Dòng 193: `order.UpdatedAt = DateTime.Now`  
- Dòng 73: `if (discount.ExpiresAt < DateTime.Now)` trong discount check

Giải thích: Deploy lên server ở timezone khác (Azure Southeast Asia = UTC+7,  
server mặc định là UTC) → `CreatedAt` sai 7 tiếng.

---

### Bước 6 — Câu hỏi để học viên suy nghĩ (kết bài)

> *"Nếu muốn test riêng rule discount — tách ra khỏi DB, khỏi email, khỏi HTTP —  
> bạn sẽ tổ chức lại code như thế nào?"*

Không cần trả lời ngay. Đây là câu hỏi xuyên suốt khóa học.

---

## Danh sách vấn đề trong code (để học viên tự tìm)

| # | Vị trí | Vấn đề |
|---|--------|--------|
| 1 | `OrdersController.PlaceOrder` | Action method làm quá nhiều thứ |
| 2 | `OrdersController.PlaceOrder` dòng 44-65 | Validation rải trong controller |
| 3 | `OrdersController.PlaceOrder` vòng lặp đầu | N+1 query ẩn |
| 4 | `OrdersController.PlaceOrder` dòng 98-105 | Business logic discount trong controller |
| 5 | `OrdersController.PlaceOrder` dòng 108-135 | Discount code logic chồng chéo |
| 6 | `OrdersController.PlaceOrder` dòng 138-147 | Order không có invariant |
| 7 | `OrdersController.PlaceOrder` vòng lặp thứ 2 | Side effect ẩn, query lặp lại |
| 8 | `OrdersController.PlaceOrder` dòng 163-191 | SMTP coupling trực tiếp trong controller |
| 9 | `OrdersController.GetOrders` | Business rule ẩn (30 ngày), không phân trang |
| 10 | `OrdersController.UpdateStatus` | State machine trong controller |
| 11 | `OrdersController.CancelOrder` | Logic duplicate với UpdateStatus |
| 12 | Nhiều chỗ | `DateTime.Now` thay vì `DateTime.UtcNow` |
| 13 | `GetOrders` và `GetOrder` | Copy-paste mapping code |

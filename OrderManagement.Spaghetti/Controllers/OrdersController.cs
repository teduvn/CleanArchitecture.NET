using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Spaghetti.Data;
using OrderManagement.Spaghetti.DTOs;
using OrderManagement.Spaghetti.Models;
using System.Net;
using System.Net.Mail;

namespace OrderManagement.Spaghetti.Controllers;

// ============================================================
// CẢNH BÁO: Đây là code DEMO cho mục đích giảng dạy.
// Controller này cố tình vi phạm mọi nguyên tắc Clean Architecture
// để minh họa các vấn đề trong thực tế.
// KHÔNG dùng pattern này trong production.
// ============================================================

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        AppDbContext db,
        IConfiguration config,
        ILogger<OrdersController> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // -------------------------------------------------------
    // VẤN ĐỀ #1: Action method làm quá nhiều thứ
    // Một method xử lý: validate + tính tiền + discount +
    // trừ stock + lưu DB + gửi email + trả response
    // -------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        // -------------------------------------------------------
        // VẤN ĐỀ #2: Validation rải trong controller
        // Mỗi endpoint phải tự validate lại từ đầu
        // Không tái sử dụng được, không test độc lập được
        // -------------------------------------------------------
        if (request.Items == null || !request.Items.Any())
            return BadRequest(new { Error = "Đơn hàng phải có ít nhất 1 sản phẩm" });

        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            return BadRequest(new { Error = "Email không được để trống" });

        if (!request.CustomerEmail.Contains("@"))
            return BadRequest(new { Error = "Email không hợp lệ" });

        if (string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { Error = "Tên khách hàng không được để trống" });

        if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            return BadRequest(new { Error = "Địa chỉ giao hàng không được để trống" });

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return BadRequest(new { Error = $"Số lượng sản phẩm {item.ProductId} phải lớn hơn 0" });

            if (item.Quantity > 100)
                return BadRequest(new { Error = $"Số lượng mỗi sản phẩm không được quá 100" });
        }

        // -------------------------------------------------------
        // VẤN ĐỀ #3: Query N+1 ẩn trong vòng lặp
        // Đơn hàng 10 sản phẩm = 10 query riêng lẻ đến DB
        // Không ai nhìn vào đây biết được điều này đang xảy ra
        // -------------------------------------------------------
        decimal subtotal = 0;
        var validatedItems = new List<(OrderItemRequest req, Product product)>();

        foreach (var item in request.Items)
        {
            // BUG TIỀM ẨN: Mỗi iteration = 1 round trip đến database
            var product = await _db.Products.FindAsync(item.ProductId);

            if (product == null)
                return BadRequest(new { Error = $"Sản phẩm ID={item.ProductId} không tồn tại" });

            if (!product.IsActive)
                return BadRequest(new { Error = $"Sản phẩm '{product.Name}' hiện không còn bán" });

            if (product.Stock < item.Quantity)
                return BadRequest(new
                {
                    Error = $"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho, " +
                            $"bạn đặt {item.Quantity}"
                });

            subtotal += product.Price * item.Quantity;
            validatedItems.Add((item, product));
        }

        // -------------------------------------------------------
        // VẤN ĐỀ #4: Business logic nằm thẳng trong controller
        // Rule: đơn > 1 triệu giảm 10%, đơn > 5 triệu giảm 15%
        // Để test rule này phải spin up cả HTTP stack + database
        // Đổi rule = phải tìm vào đây để sửa
        // -------------------------------------------------------
        decimal discountAmount = 0;
        string? appliedDiscountCode = null;

        // Discount tự động theo giá trị đơn hàng
        if (subtotal >= 5_000_000)
        {
            discountAmount = subtotal * 0.15m; // Giảm 15% cho đơn >= 5 triệu
        }
        else if (subtotal >= 1_000_000)
        {
            discountAmount = subtotal * 0.10m; // Giảm 10% cho đơn >= 1 triệu
        }

        // -------------------------------------------------------
        // VẤN ĐỀ #5: Discount code logic cũng nằm ở đây
        // Nếu có cả discount tự động lẫn discount code thì sao?
        // Rule chồng rule, không ai quản lý được
        // -------------------------------------------------------
        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            // BUG: Thêm 1 query DB nữa
            var discount = await _db.DiscountCodes
                .FirstOrDefaultAsync(d => d.Code == request.DiscountCode && d.IsActive);

            if (discount == null)
                return BadRequest(new { Error = $"Mã giảm giá '{request.DiscountCode}' không hợp lệ" });

            if (discount.ExpiresAt < DateTime.Now) // BUG: Nên dùng UtcNow
                return BadRequest(new { Error = "Mã giảm giá đã hết hạn" });

            if (subtotal < discount.MinOrderAmount)
                return BadRequest(new
                {
                    Error = $"Đơn hàng tối thiểu {discount.MinOrderAmount:N0} VND để dùng mã này"
                });

            if (discount.UsageCount >= discount.MaxUsage)
                return BadRequest(new { Error = "Mã giảm giá đã hết lượt sử dụng" });

            // BUG: Discount code override discount tự động, hay cộng dồn?
            // Không có rule rõ ràng, developer tự quyết định
            var codeDiscount = subtotal * (discount.DiscountPercent / 100);
            if (codeDiscount > discountAmount)
            {
                discountAmount = codeDiscount;
                appliedDiscountCode = discount.Code;
            }

            // BUG RACE CONDITION: Đọc count rồi mới tăng
            // Nếu 2 request đến cùng lúc cùng dùng 1 mã → cả 2 đều pass
            discount.UsageCount++;
        }

        decimal total = subtotal - discountAmount;

        // -------------------------------------------------------
        // VẤN ĐỀ #6: Tạo Order object nhưng không có invariant
        // Không gì ngăn Order.Total bị âm
        // Không gì ngăn Status nhận giá trị tùy ý như "Foo"
        // -------------------------------------------------------
        var order = new Order
        {
            CustomerEmail = request.CustomerEmail,
            CustomerName = request.CustomerName,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            DiscountCode = appliedDiscountCode,
            DiscountAmount = discountAmount,
            Total = total,
            Status = "Pending",
            CreatedAt = DateTime.Now, // BUG: Nên là UtcNow — sẽ bị sai ở server timezone khác
        };

        // -------------------------------------------------------
        // VẤN ĐỀ #7: Side effect ẩn — trừ stock lẫn vào logic tạo OrderItem
        // Ai đọc code này 6 tháng sau có thể bỏ sót dòng này
        // Nếu email gửi fail ở dưới, stock đã bị trừ nhưng order chưa lưu
        // -------------------------------------------------------
        foreach (var (req, product) in validatedItems)
        {
            // BUG: Query DB lần 2 cho cùng 1 product — đã query ở trên rồi
            var p = await _db.Products.FindAsync(req.ProductId)!;

            order.Items.Add(new OrderItem
            {
                ProductId = req.ProductId,
                Quantity = req.Quantity,
                UnitPrice = product.Price,
                Subtotal = product.Price * req.Quantity
            });

            p!.Stock -= req.Quantity; // Side effect ẩn: trừ stock
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // -------------------------------------------------------
        // VẤN ĐỀ #8: Infrastructure concern (SMTP) nằm thẳng trong controller
        // Muốn test PlaceOrder mà không gửi email thật → không làm được
        // Đổi sang SendGrid → phải vào sửa controller
        // Email fail → exception leak ra response
        // -------------------------------------------------------
        try
        {
            var smtpHost = _config["Smtp:Host"] ?? "localhost";
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "25");
            var smtpUser = _config["Smtp:Username"] ?? "";
            var smtpPass = _config["Smtp:Password"] ?? "";

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.EnableSsl = true;

            var mail = new MailMessage
            {
                From = new MailAddress("no-reply@shop.com"),
                Subject = $"Xác nhận đơn hàng #{order.Id}",
                Body = $"""
                    Xin chào {order.CustomerName},
                    
                    Đơn hàng #{order.Id} của bạn đã được đặt thành công.
                    Tổng tiền: {total:N0} VND
                    {(discountAmount > 0 ? $"Tiết kiệm: {discountAmount:N0} VND" : "")}
                    
                    Địa chỉ giao hàng: {order.ShippingAddress}
                    
                    Chúng tôi sẽ liên hệ xác nhận trong vòng 24 giờ.
                    """,
                IsBodyHtml = false
            };
            mail.To.Add(request.CustomerEmail);

            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            // BUG: Log lỗi nhưng không làm gì — order đã lưu nhưng email không gửi được
            // Khách hàng không biết, team không biết
            _logger.LogError(ex, "Không gửi được email xác nhận cho đơn hàng {OrderId}", order.Id);
        }

        _logger.LogInformation(
            "Order #{OrderId} placed by {Email}, total: {Total}",
            order.Id, order.CustomerEmail, total);

        return Ok(new
        {
            OrderId = order.Id,
            Total = total,
            DiscountAmount = discountAmount,
            Message = "Đặt hàng thành công"
        });
    }

    // -------------------------------------------------------
    // VẤN ĐỀ #9: GetOrders — business logic lại nằm trong controller
    // Rule "chỉ trả về order trong 30 ngày gần nhất nếu không có filter"
    // bị chôn vào query LINQ — không ai biết rule này tồn tại
    // -------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? email,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        // BUG: Không có phân trang — trả về tất cả orders
        // 1 triệu đơn hàng = OutOfMemoryException
        var query = _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(o => o.CustomerEmail == email);

        // Business rule ẩn: mặc định chỉ lấy 30 ngày gần nhất
        if (from == null && to == null)
            query = query.Where(o => o.CreatedAt >= DateTime.Now.AddDays(-30)); // BUG: DateTime.Now

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

        // BUG: Map thủ công, lặp đi lặp lại ở mọi endpoint
        var result = orders.Select(o => new OrderResponse
        {
            Id = o.Id,
            CustomerEmail = o.CustomerEmail,
            CustomerName = o.CustomerName,
            Status = o.Status,
            Total = o.Total,
            DiscountAmount = o.DiscountAmount,
            DiscountCode = o.DiscountCode,
            ShippingAddress = o.ShippingAddress,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Unknown",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList()
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { Error = $"Không tìm thấy đơn hàng #{id}" });

        // BUG: Copy paste mapping từ GetOrders — vi phạm DRY
        return Ok(new OrderResponse
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            CustomerName = order.CustomerName,
            Status = order.Status,
            Total = order.Total,
            DiscountAmount = order.DiscountAmount,
            DiscountCode = order.DiscountCode,
            ShippingAddress = order.ShippingAddress,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? "Unknown",
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList()
        });
    }

    // -------------------------------------------------------
    // VẤN ĐỀ #10: UpdateStatus — state machine logic trong controller
    // Rule chuyển trạng thái (Pending→Confirmed→Shipped→Cancelled)
    // bị chôn ở đây. Nếu có 3 endpoint khác cũng cần validate
    // transition thì phải copy-paste logic này.
    // -------------------------------------------------------
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound(new { Error = $"Không tìm thấy đơn hàng #{id}" });

        // State machine logic nằm thẳng trong controller
        var allowedTransitions = new Dictionary<string, List<string>>
        {
            ["Pending"]   = new() { "Confirmed", "Cancelled" },
            ["Confirmed"] = new() { "Shipped", "Cancelled" },
            ["Shipped"]   = new() { },
            ["Cancelled"] = new() { }
        };

        if (!allowedTransitions.TryGetValue(order.Status, out var allowed))
            return BadRequest(new { Error = $"Trạng thái '{order.Status}' không hợp lệ" });

        if (!allowed.Contains(request.Status))
            return BadRequest(new
            {
                Error = $"Không thể chuyển từ '{order.Status}' sang '{request.Status}'"
            });

        var previousStatus = order.Status;
        order.Status = request.Status;
        order.UpdatedAt = DateTime.Now; // BUG: Nên là UtcNow
        if (request.Notes != null) order.Notes = request.Notes;

        await _db.SaveChangesAsync();

        // BUG: Nếu cần gửi email khi Shipped → copy paste SmtpClient ở đây
        // Không có cách nào tái sử dụng logic gửi email từ PlaceOrder
        if (request.Status == "Shipped")
        {
            _logger.LogInformation(
                "Order #{OrderId} shipped — should send email but email code is duplicated",
                order.Id);
            // TODO: copy paste SmtpClient code từ PlaceOrder xuống đây...
        }

        _logger.LogInformation(
            "Order #{OrderId} status changed: {From} → {To}",
            order.Id, previousStatus, request.Status);

        return Ok(new { Message = $"Cập nhật trạng thái thành công: {request.Status}" });
    }

    // -------------------------------------------------------
    // VẤN ĐỀ #11: CancelOrder — duplicate logic với UpdateStatus
    // Có 2 cách cancel order trong cùng 1 controller
    // -------------------------------------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelOrder(int id, [FromQuery] string? reason)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { Error = $"Không tìm thấy đơn hàng #{id}" });

        // BUG: Duplicate logic — UpdateStatus cũng check state transition
        if (order.Status == "Shipped")
            return BadRequest(new { Error = "Không thể hủy đơn hàng đã giao" });

        if (order.Status == "Cancelled")
            return BadRequest(new { Error = "Đơn hàng đã bị hủy trước đó" });

        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;
        order.Notes = reason ?? "Hủy bởi khách hàng";

        // BUG: Hoàn lại stock khi cancel — logic này không nằm cùng với
        // logic trừ stock ở PlaceOrder. Developer mới sẽ không biết
        // 2 chỗ này liên quan đến nhau.
        foreach (var item in order.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product != null)
                product.Stock += item.Quantity; // Hoàn lại stock
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Đơn hàng đã được hủy" });
    }
}

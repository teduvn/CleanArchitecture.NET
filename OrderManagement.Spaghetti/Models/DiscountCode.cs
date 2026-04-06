namespace OrderManagement.Spaghetti.Models;

public class DiscountCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public decimal MinOrderAmount { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    public int MaxUsage { get; set; } = 100;
}

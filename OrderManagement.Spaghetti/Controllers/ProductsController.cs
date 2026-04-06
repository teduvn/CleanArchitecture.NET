using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Spaghetti.Data;
using OrderManagement.Spaghetti.Models;

namespace OrderManagement.Spaghetti.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(AppDbContext db, ILogger<ProductsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] string? category, [FromQuery] bool? inStock)
    {
        var query = _db.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (inStock == true)
            query = query.Where(p => p.Stock > 0);

        var products = await query.Where(p => p.IsActive).ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] Product product)
    {
        // BUG: Không validate gì cả — Price có thể âm, Stock có thể âm
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        // BUG: Không kiểm tra xem các field nào thực sự thay đổi
        product.Name = request.Name;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.Category = request.Category;
        product.Description = request.Description;
        product.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return Ok(product);
    }
}

using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync();
    }

    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        return product;
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto dto)
    {
        // Check if SKU already exists
        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.SKU == dto.SKU);
        
        if (existingProduct != null)
        {
            return BadRequest(new { message = $"Product with SKU '{dto.SKU}' already exists." });
        }

        // Validate CategoryId if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId.Value);
            
            if (!categoryExists)
            {
                return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
            }
        }

        // Validate SupplierId if provided
        if (dto.SupplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == dto.SupplierId.Value);
            
            if (!supplierExists)
            {
                return BadRequest(new { message = $"Supplier with ID {dto.SupplierId} does not exist." });
            }
        }

        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId,
            UnitPrice = dto.UnitPrice,
            IsActive = dto.IsActive
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Load category for response
        await _context.Entry(product)
            .Reference(p => p.Category)
            .LoadAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
    }

    // PUT: api/products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        // Check if SKU is being changed and if new SKU already exists
        if (product.SKU != dto.SKU)
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == dto.SKU && p.ProductId != id);
            
            if (existingProduct != null)
            {
                return BadRequest(new { message = $"Product with SKU '{dto.SKU}' already exists." });
            }
        }

        // Validate CategoryId if provided
        if (dto.CategoryId.HasValue)
        {
            var categoryExists = await _context.Categories
                .AnyAsync(c => c.CategoryId == dto.CategoryId.Value);
            
            if (!categoryExists)
            {
                return BadRequest(new { message = $"Category with ID {dto.CategoryId} does not exist." });
            }
        }

        // Validate SupplierId if provided
        if (dto.SupplierId.HasValue)
        {
            var supplierExists = await _context.Suppliers
                .AnyAsync(s => s.SupplierId == dto.SupplierId.Value);
            
            if (!supplierExists)
            {
                return BadRequest(new { message = $"Supplier with ID {dto.SupplierId} does not exist." });
            }
        }

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.Description = dto.Description;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;
        product.UnitPrice = dto.UnitPrice;
        product.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        
        if (product == null)
        {
            return NotFound(new { message = $"Product with ID {id} not found." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
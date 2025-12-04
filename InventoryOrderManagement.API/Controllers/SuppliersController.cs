using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/suppliers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
    {
        return await _context.Suppliers
            .Include(s => s.Products)
            .Include(s => s.PurchaseOrders)
            .ToListAsync();
    }

    // GET: api/suppliers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Supplier>> GetSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.SupplierId == id);

        if (supplier == null)
        {
            return NotFound(new { message = $"Supplier with ID {id} not found." });
        }

        return supplier;
    }

    // POST: api/suppliers
    [HttpPost]
    public async Task<ActionResult<Supplier>> CreateSupplier(CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.SupplierId }, supplier);
    }

    // PUT: api/suppliers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSupplier(int id, UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier == null)
        {
            return NotFound(new { message = $"Supplier with ID {id} not found." });
        }

        supplier.Name = dto.Name;
        supplier.ContactPerson = dto.ContactPerson;
        supplier.Email = dto.Email;
        supplier.Phone = dto.Phone;
        supplier.Address = dto.Address;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/suppliers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSupplier(int id)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Products)
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.SupplierId == id);
        
        if (supplier == null)
        {
            return NotFound(new { message = $"Supplier with ID {id} not found." });
        }

        // Check if supplier has products
        if (supplier.Products.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete supplier. It has {supplier.Products.Count} product(s) associated with it." 
            });
        }

        // Check if supplier has purchase orders
        if (supplier.PurchaseOrders.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete supplier. It has {supplier.PurchaseOrders.Count} purchase order(s) associated with it." 
            });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

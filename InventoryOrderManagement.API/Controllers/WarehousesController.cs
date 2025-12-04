using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehousesController : ControllerBase
{
    private readonly AppDbContext _context;

    public WarehousesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/warehouses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Warehouse>>> GetWarehouses()
    {
        return await _context.Warehouses
            .Include(w => w.Locations)
            .Include(w => w.Inventories)
            .ToListAsync();
    }

    // GET: api/warehouses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Warehouse>> GetWarehouse(int id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Locations)
            .Include(w => w.Inventories)
            .FirstOrDefaultAsync(w => w.WarehouseId == id);

        if (warehouse == null)
        {
            return NotFound(new { message = $"Warehouse with ID {id} not found." });
        }

        return warehouse;
    }

    // POST: api/warehouses
    [HttpPost]
    public async Task<ActionResult<Warehouse>> CreateWarehouse(CreateWarehouseDto dto)
    {
        var warehouse = new Warehouse
        {
            Name = dto.Name,
            Address = dto.Address
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.WarehouseId }, warehouse);
    }

    // PUT: api/warehouses/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWarehouse(int id, UpdateWarehouseDto dto)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);

        if (warehouse == null)
        {
            return NotFound(new { message = $"Warehouse with ID {id} not found." });
        }

        warehouse.Name = dto.Name;
        warehouse.Address = dto.Address;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/warehouses/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.Locations)
            .Include(w => w.Inventories)
            .FirstOrDefaultAsync(w => w.WarehouseId == id);
        
        if (warehouse == null)
        {
            return NotFound(new { message = $"Warehouse with ID {id} not found." });
        }

        // Check if warehouse has locations
        if (warehouse.Locations.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete warehouse. It has {warehouse.Locations.Count} location(s) associated with it." 
            });
        }

        // Check if warehouse has inventories
        if (warehouse.Inventories.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete warehouse. It has {warehouse.Inventories.Count} inventory record(s) associated with it." 
            });
        }

        _context.Warehouses.Remove(warehouse);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


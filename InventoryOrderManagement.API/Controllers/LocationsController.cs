using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocationsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/locations
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
    {
        return await _context.Locations
            .Include(l => l.Warehouse)
            .Include(l => l.Inventories)
            .ToListAsync();
    }

    // GET: api/locations/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Location>> GetLocation(int id)
    {
        var location = await _context.Locations
            .Include(l => l.Warehouse)
            .Include(l => l.Inventories)
            .FirstOrDefaultAsync(l => l.LocationId == id);

        if (location == null)
        {
            return NotFound(new { message = $"Location with ID {id} not found." });
        }

        return location;
    }

    // GET: api/locations/warehouse/5
    [HttpGet("warehouse/{warehouseId}")]
    public async Task<ActionResult<IEnumerable<Location>>> GetLocationsByWarehouse(int warehouseId)
    {
        var locations = await _context.Locations
            .Include(l => l.Warehouse)
            .Include(l => l.Inventories)
            .Where(l => l.WarehouseId == warehouseId)
            .ToListAsync();

        return locations;
    }

    // POST: api/locations
    [HttpPost]
    public async Task<ActionResult<Location>> CreateLocation(CreateLocationDto dto)
    {
        // Validate WarehouseId
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == dto.WarehouseId);
        
        if (!warehouseExists)
        {
            return BadRequest(new { message = $"Warehouse with ID {dto.WarehouseId} does not exist." });
        }

        var location = new Location
        {
            WarehouseId = dto.WarehouseId,
            Code = dto.Code,
            Description = dto.Description
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(location)
            .Reference(l => l.Warehouse)
            .LoadAsync();

        return CreatedAtAction(nameof(GetLocation), new { id = location.LocationId }, location);
    }

    // PUT: api/locations/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateLocation(int id, UpdateLocationDto dto)
    {
        var location = await _context.Locations.FindAsync(id);

        if (location == null)
        {
            return NotFound(new { message = $"Location with ID {id} not found." });
        }

        // Validate WarehouseId
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == dto.WarehouseId);
        
        if (!warehouseExists)
        {
            return BadRequest(new { message = $"Warehouse with ID {dto.WarehouseId} does not exist." });
        }

        location.WarehouseId = dto.WarehouseId;
        location.Code = dto.Code;
        location.Description = dto.Description;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/locations/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLocation(int id)
    {
        var location = await _context.Locations
            .Include(l => l.Inventories)
            .FirstOrDefaultAsync(l => l.LocationId == id);
        
        if (location == null)
        {
            return NotFound(new { message = $"Location with ID {id} not found." });
        }

        // Check if location has inventories
        if (location.Inventories.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete location. It has {location.Inventories.Count} inventory record(s) associated with it." 
            });
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


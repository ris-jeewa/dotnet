using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using InventoryOrderManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;

    public InventoriesController(AppDbContext context, InventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    // GET: api/inventories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Inventory>>> GetInventories()
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Include(i => i.Location)
            .ToListAsync();
    }

    // GET: api/inventories/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Inventory>> GetInventory(int id)
    {
        var inventory = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Include(i => i.Location)
            .FirstOrDefaultAsync(i => i.InventoryId == id);

        if (inventory == null)
        {
            return NotFound(new { message = $"Inventory with ID {id} not found." });
        }

        return inventory;
    }

    // GET: api/inventories/product/5
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<Inventory>>> GetInventoriesByProduct(int productId)
    {
        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Include(i => i.Location)
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        return inventories;
    }

    // GET: api/inventories/warehouse/5
    [HttpGet("warehouse/{warehouseId}")]
    public async Task<ActionResult<IEnumerable<Inventory>>> GetInventoriesByWarehouse(int warehouseId)
    {
        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Include(i => i.Location)
            .Where(i => i.WarehouseId == warehouseId)
            .ToListAsync();

        return inventories;
    }

    // POST: api/inventories
    [HttpPost]
    public async Task<ActionResult<Inventory>> CreateInventory(CreateInventoryDto dto)
    {
        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        // Validate WarehouseId
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == dto.WarehouseId);
        
        if (!warehouseExists)
        {
            return BadRequest(new { message = $"Warehouse with ID {dto.WarehouseId} does not exist." });
        }

        // Validate LocationId if provided
        if (dto.LocationId.HasValue)
        {
            var locationExists = await _context.Locations
                .AnyAsync(l => l.LocationId == dto.LocationId.Value && l.WarehouseId == dto.WarehouseId);
            
            if (!locationExists)
            {
                return BadRequest(new { message = $"Location with ID {dto.LocationId} does not exist in the specified warehouse." });
            }
        }

        // Check if inventory already exists for this product in this warehouse
        var existingInventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId && i.WarehouseId == dto.WarehouseId);
        
        if (existingInventory != null)
        {
            return BadRequest(new { message = $"Inventory already exists for Product ID {dto.ProductId} in Warehouse ID {dto.WarehouseId}. Use PUT to update." });
        }

        // Validate quantity - prevent negative stock
        try
        {
            await _inventoryService.ValidateInventoryQuantityAsync(0, dto.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var inventory = new Inventory
        {
            ProductId = dto.ProductId,
            WarehouseId = dto.WarehouseId,
            LocationId = dto.LocationId,
            Quantity = dto.Quantity,
            ReorderLevel = dto.ReorderLevel,
            LastUpdated = DateTime.UtcNow
        };

        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(inventory)
            .Reference(i => i.Product)
            .LoadAsync();
        await _context.Entry(inventory)
            .Reference(i => i.Warehouse)
            .LoadAsync();
        if (inventory.LocationId.HasValue)
        {
            await _context.Entry(inventory)
                .Reference(i => i.Location)
                .LoadAsync();
        }

        return CreatedAtAction(nameof(GetInventory), new { id = inventory.InventoryId }, inventory);
    }

    // PUT: api/inventories/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInventory(int id, UpdateInventoryDto dto)
    {
        var inventory = await _context.Inventories.FindAsync(id);

        if (inventory == null)
        {
            return NotFound(new { message = $"Inventory with ID {id} not found." });
        }

        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        // Validate WarehouseId
        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.WarehouseId == dto.WarehouseId);
        
        if (!warehouseExists)
        {
            return BadRequest(new { message = $"Warehouse with ID {dto.WarehouseId} does not exist." });
        }

        // Validate LocationId if provided
        if (dto.LocationId.HasValue)
        {
            var locationExists = await _context.Locations
                .AnyAsync(l => l.LocationId == dto.LocationId.Value && l.WarehouseId == dto.WarehouseId);
            
            if (!locationExists)
            {
                return BadRequest(new { message = $"Location with ID {dto.LocationId} does not exist in the specified warehouse." });
            }
        }

        // Check if another inventory exists for this product-warehouse combination
        var existingInventory = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == dto.ProductId 
                && i.WarehouseId == dto.WarehouseId 
                && i.InventoryId != id);
        
        if (existingInventory != null)
        {
            return BadRequest(new { message = $"Another inventory already exists for Product ID {dto.ProductId} in Warehouse ID {dto.WarehouseId}." });
        }

        // Validate quantity - prevent negative stock
        try
        {
            await _inventoryService.ValidateInventoryQuantityAsync(id, dto.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        inventory.ProductId = dto.ProductId;
        inventory.WarehouseId = dto.WarehouseId;
        inventory.LocationId = dto.LocationId;
        inventory.Quantity = dto.Quantity;
        inventory.ReorderLevel = dto.ReorderLevel;
        inventory.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/inventories/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteInventory(int id)
    {
        var inventory = await _context.Inventories.FindAsync(id);
        
        if (inventory == null)
        {
            return NotFound(new { message = $"Inventory with ID {id} not found." });
        }

        _context.Inventories.Remove(inventory);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


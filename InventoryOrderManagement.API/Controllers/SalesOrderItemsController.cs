using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using InventoryOrderManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesOrderItemsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;

    public SalesOrderItemsController(AppDbContext context, InventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    // GET: api/salesorderitems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrderItem>>> GetSalesOrderItems()
    {
        return await _context.SalesOrderItems
            .Include(item => item.SalesOrder)
                .ThenInclude(so => so.Customer)
            .Include(item => item.Product)
            .ToListAsync();
    }

    // GET: api/salesorderitems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrderItem>> GetSalesOrderItem(int id)
    {
        var item = await _context.SalesOrderItems
            .Include(i => i.SalesOrder)
                .ThenInclude(so => so.Customer)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.SalesOrderItemId == id);

        if (item == null)
        {
            return NotFound(new { message = $"Sales Order Item with ID {id} not found." });
        }

        return item;
    }

    // GET: api/salesorderitems/salesorder/5
    [HttpGet("salesorder/{salesOrderId}")]
    public async Task<ActionResult<IEnumerable<SalesOrderItem>>> GetSalesOrderItemsBySalesOrder(int salesOrderId)
    {
        var items = await _context.SalesOrderItems
            .Include(item => item.SalesOrder)
            .Include(item => item.Product)
            .Where(item => item.SalesOrderId == salesOrderId)
            .ToListAsync();

        return items;
    }

    // GET: api/salesorderitems/product/5
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<SalesOrderItem>>> GetSalesOrderItemsByProduct(int productId)
    {
        var items = await _context.SalesOrderItems
            .Include(item => item.SalesOrder)
                .ThenInclude(so => so.Customer)
            .Include(item => item.Product)
            .Where(item => item.ProductId == productId)
            .ToListAsync();

        return items;
    }

    // POST: api/salesorderitems
    [HttpPost]
    public async Task<ActionResult<SalesOrderItem>> CreateSalesOrderItem(CreateSalesOrderItemDto dto)
    {
        // Validate SalesOrderId
        var salesOrder = await _context.SalesOrders.FindAsync(dto.SalesOrderId);
        
        if (salesOrder == null)
        {
            return BadRequest(new { message = $"Sales Order with ID {dto.SalesOrderId} does not exist." });
        }

        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        // Validate stock availability - prevent negative stock
        try
        {
            await _inventoryService.ValidateStockAvailabilityAsync(dto.ProductId, dto.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var item = new SalesOrderItem
        {
            SalesOrderId = dto.SalesOrderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };

        _context.SalesOrderItems.Add(item);
        
        // Recalculate sales order total amount
        var allItems = await _context.SalesOrderItems
            .Where(i => i.SalesOrderId == dto.SalesOrderId)
            .ToListAsync();
        
        salesOrder.TotalAmount = allItems.Sum(i => i.Quantity * i.UnitPrice) + (dto.Quantity * dto.UnitPrice);
        
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(item)
            .Reference(i => i.SalesOrder)
            .LoadAsync();
        await _context.Entry(item)
            .Reference(i => i.Product)
            .LoadAsync();

        return CreatedAtAction(nameof(GetSalesOrderItem), new { id = item.SalesOrderItemId }, item);
    }

    // PUT: api/salesorderitems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSalesOrderItem(int id, UpdateSalesOrderItemDto dto)
    {
        var item = await _context.SalesOrderItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = $"Sales Order Item with ID {id} not found." });
        }

        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        // Calculate the net change in quantity
        var quantityChange = dto.Quantity - item.Quantity;
        
        // If increasing quantity, validate stock availability
        if (quantityChange > 0)
        {
            try
            {
                await _inventoryService.ValidateStockAvailabilityAsync(dto.ProductId, quantityChange);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Store old values for total recalculation
        var oldTotal = item.Quantity * item.UnitPrice;

        item.ProductId = dto.ProductId;
        item.Quantity = dto.Quantity;
        item.UnitPrice = dto.UnitPrice;

        // Recalculate sales order total amount
        var salesOrder = await _context.SalesOrders.FindAsync(item.SalesOrderId);
        var allItems = await _context.SalesOrderItems
            .Where(i => i.SalesOrderId == item.SalesOrderId)
            .ToListAsync();
        
        salesOrder!.TotalAmount = allItems.Sum(i => i.Quantity * i.UnitPrice) - oldTotal + (dto.Quantity * dto.UnitPrice);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/salesorderitems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalesOrderItem(int id)
    {
        var item = await _context.SalesOrderItems.FindAsync(id);
        
        if (item == null)
        {
            return NotFound(new { message = $"Sales Order Item with ID {id} not found." });
        }

        var salesOrderId = item.SalesOrderId;
        var itemTotal = item.Quantity * item.UnitPrice;

        _context.SalesOrderItems.Remove(item);
        
        // Recalculate sales order total amount
        var salesOrder = await _context.SalesOrders.FindAsync(salesOrderId);
        var remainingItems = await _context.SalesOrderItems
            .Where(i => i.SalesOrderId == salesOrderId)
            .ToListAsync();
        
        salesOrder!.TotalAmount = remainingItems.Sum(i => i.Quantity * i.UnitPrice);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


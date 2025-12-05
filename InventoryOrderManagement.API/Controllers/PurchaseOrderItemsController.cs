using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrderItemsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseOrderItemsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/purchaseorderitems
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrderItem>>> GetPurchaseOrderItems()
    {
        return await _context.PurchaseOrderItems
            .Include(item => item.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(item => item.Product)
            .ToListAsync();
    }

    // GET: api/purchaseorderitems/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderItem>> GetPurchaseOrderItem(int id)
    {
        var item = await _context.PurchaseOrderItems
            .Include(i => i.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.PurchaseOrderItemId == id);

        if (item == null)
        {
            return NotFound(new { message = $"Purchase Order Item with ID {id} not found." });
        }

        return item;
    }

    // GET: api/purchaseorderitems/purchaseorder/5
    [HttpGet("purchaseorder/{purchaseOrderId}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderItem>>> GetPurchaseOrderItemsByPurchaseOrder(int purchaseOrderId)
    {
        var items = await _context.PurchaseOrderItems
            .Include(item => item.PurchaseOrder)
            .Include(item => item.Product)
            .Where(item => item.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();

        return items;
    }

    // GET: api/purchaseorderitems/product/5
    [HttpGet("product/{productId}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderItem>>> GetPurchaseOrderItemsByProduct(int productId)
    {
        var items = await _context.PurchaseOrderItems
            .Include(item => item.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(item => item.Product)
            .Where(item => item.ProductId == productId)
            .ToListAsync();

        return items;
    }

    // POST: api/purchaseorderitems
    [HttpPost]
    public async Task<ActionResult<PurchaseOrderItem>> CreatePurchaseOrderItem(CreatePurchaseOrderItemDto dto)
    {
        // Validate PurchaseOrderId
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(dto.PurchaseOrderId);
        
        if (purchaseOrder == null)
        {
            return BadRequest(new { message = $"Purchase Order with ID {dto.PurchaseOrderId} does not exist." });
        }

        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        var item = new PurchaseOrderItem
        {
            PurchaseOrderId = dto.PurchaseOrderId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };

        _context.PurchaseOrderItems.Add(item);
        
        // Recalculate purchase order total amount
        var allItems = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == dto.PurchaseOrderId)
            .ToListAsync();
        
        purchaseOrder.TotalAmount = allItems.Sum(i => i.Quantity * i.UnitPrice) + (dto.Quantity * dto.UnitPrice);
        
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(item)
            .Reference(i => i.PurchaseOrder)
            .LoadAsync();
        await _context.Entry(item)
            .Reference(i => i.Product)
            .LoadAsync();

        return CreatedAtAction(nameof(GetPurchaseOrderItem), new { id = item.PurchaseOrderItemId }, item);
    }

    // PUT: api/purchaseorderitems/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseOrderItem(int id, UpdatePurchaseOrderItemDto dto)
    {
        var item = await _context.PurchaseOrderItems.FindAsync(id);

        if (item == null)
        {
            return NotFound(new { message = $"Purchase Order Item with ID {id} not found." });
        }

        // Validate ProductId
        var productExists = await _context.Products
            .AnyAsync(p => p.ProductId == dto.ProductId);
        
        if (!productExists)
        {
            return BadRequest(new { message = $"Product with ID {dto.ProductId} does not exist." });
        }

        // Store old values for total recalculation
        var oldTotal = item.Quantity * item.UnitPrice;

        item.ProductId = dto.ProductId;
        item.Quantity = dto.Quantity;
        item.UnitPrice = dto.UnitPrice;

        // Recalculate purchase order total amount
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(item.PurchaseOrderId);
        var allItems = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == item.PurchaseOrderId)
            .ToListAsync();
        
        purchaseOrder!.TotalAmount = allItems.Sum(i => i.Quantity * i.UnitPrice) - oldTotal + (dto.Quantity * dto.UnitPrice);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/purchaseorderitems/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrderItem(int id)
    {
        var item = await _context.PurchaseOrderItems.FindAsync(id);
        
        if (item == null)
        {
            return NotFound(new { message = $"Purchase Order Item with ID {id} not found." });
        }

        var purchaseOrderId = item.PurchaseOrderId;
        var itemTotal = item.Quantity * item.UnitPrice;

        _context.PurchaseOrderItems.Remove(item);
        
        // Recalculate purchase order total amount
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
        var remainingItems = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == purchaseOrderId)
            .ToListAsync();
        
        purchaseOrder!.TotalAmount = remainingItems.Sum(i => i.Quantity * i.UnitPrice);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


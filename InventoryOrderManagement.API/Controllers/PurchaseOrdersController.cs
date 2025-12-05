using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseOrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/purchaseorders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrders()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(item => item.Product)
            .ToListAsync();
    }

    // GET: api/purchaseorders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrder>> GetPurchaseOrder(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == id);

        if (purchaseOrder == null)
        {
            return NotFound(new { message = $"Purchase Order with ID {id} not found." });
        }

        return purchaseOrder;
    }

    // GET: api/purchaseorders/supplier/5
    [HttpGet("supplier/{supplierId}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrdersBySupplier(int supplierId)
    {
        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(item => item.Product)
            .Where(po => po.SupplierId == supplierId)
            .ToListAsync();

        return purchaseOrders;
    }

    // GET: api/purchaseorders/status/Pending
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrder>>> GetPurchaseOrdersByStatus(string status)
    {
        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
                .ThenInclude(item => item.Product)
            .Where(po => po.Status == status)
            .ToListAsync();

        return purchaseOrders;
    }

    // POST: api/purchaseorders
    [HttpPost]
    public async Task<ActionResult<PurchaseOrder>> CreatePurchaseOrder(CreatePurchaseOrderDto dto)
    {
        // Validate SupplierId
        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.SupplierId == dto.SupplierId);
        
        if (!supplierExists)
        {
            return BadRequest(new { message = $"Supplier with ID {dto.SupplierId} does not exist." });
        }

        // Validate items
        if (dto.Items == null || !dto.Items.Any())
        {
            return BadRequest(new { message = "Purchase order must have at least one item." });
        }

        // Validate all products exist
        var productIds = dto.Items.Select(i => i.ProductId).Distinct().ToList();
        var existingProducts = await _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .Select(p => p.ProductId)
            .ToListAsync();

        var missingProducts = productIds.Except(existingProducts).ToList();
        if (missingProducts.Any())
        {
            return BadRequest(new { message = $"Products with IDs {string.Join(", ", missingProducts)} do not exist." });
        }

        // Calculate total amount
        var totalAmount = dto.Items.Sum(item => item.Quantity * item.UnitPrice);

        // Create purchase order
        var purchaseOrder = new PurchaseOrder
        {
            SupplierId = dto.SupplierId,
            OrderDate = dto.OrderDate ?? DateTime.UtcNow,
            Status = dto.Status,
            TotalAmount = totalAmount
        };

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        // Create purchase order items
        var purchaseOrderItems = dto.Items.Select(item => new PurchaseOrderItem
        {
            PurchaseOrderId = purchaseOrder.PurchaseOrderId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        _context.PurchaseOrderItems.AddRange(purchaseOrderItems);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(purchaseOrder)
            .Reference(po => po.Supplier)
            .LoadAsync();
        await _context.Entry(purchaseOrder)
            .Collection(po => po.PurchaseOrderItems)
            .LoadAsync();

        foreach (var item in purchaseOrderItems)
        {
            await _context.Entry(item)
                .Reference(i => i.Product)
                .LoadAsync();
        }

        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.PurchaseOrderId }, purchaseOrder);
    }

    // PUT: api/purchaseorders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseOrder(int id, UpdatePurchaseOrderDto dto)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);

        if (purchaseOrder == null)
        {
            return NotFound(new { message = $"Purchase Order with ID {id} not found." });
        }

        // Validate SupplierId
        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.SupplierId == dto.SupplierId);
        
        if (!supplierExists)
        {
            return BadRequest(new { message = $"Supplier with ID {dto.SupplierId} does not exist." });
        }

        purchaseOrder.SupplierId = dto.SupplierId;
        purchaseOrder.OrderDate = dto.OrderDate;
        purchaseOrder.Status = dto.Status;

        // Recalculate total amount from items
        var items = await _context.PurchaseOrderItems
            .Where(item => item.PurchaseOrderId == id)
            .ToListAsync();
        
        purchaseOrder.TotalAmount = items.Sum(item => item.Quantity * item.UnitPrice);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/purchaseorders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrder(int id)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        
        if (purchaseOrder == null)
        {
            return NotFound(new { message = $"Purchase Order with ID {id} not found." });
        }

        // Items will be deleted automatically due to cascade delete
        _context.PurchaseOrders.Remove(purchaseOrder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/purchaseorders/5/items
    [HttpPost("{id}/items")]
    public async Task<ActionResult<PurchaseOrderItem>> AddPurchaseOrderItem(int id, CreatePurchaseOrderItemDto dto)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        
        if (purchaseOrder == null)
        {
            return NotFound(new { message = $"Purchase Order with ID {id} not found." });
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
            PurchaseOrderId = id,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };

        _context.PurchaseOrderItems.Add(item);
        
        // Recalculate total amount
        var items = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == id)
            .ToListAsync();
        
        purchaseOrder.TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice) + (dto.Quantity * dto.UnitPrice);
        
        await _context.SaveChangesAsync();

        // Load navigation properties
        await _context.Entry(item)
            .Reference(i => i.Product)
            .LoadAsync();

        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = purchaseOrder.PurchaseOrderId }, item);
    }

    // DELETE: api/purchaseorders/5/items/10
    [HttpDelete("{id}/items/{itemId}")]
    public async Task<IActionResult> DeletePurchaseOrderItem(int id, int itemId)
    {
        var item = await _context.PurchaseOrderItems
            .FirstOrDefaultAsync(i => i.PurchaseOrderItemId == itemId && i.PurchaseOrderId == id);
        
        if (item == null)
        {
            return NotFound(new { message = $"Purchase Order Item with ID {itemId} not found in Purchase Order {id}." });
        }

        var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
        
        _context.PurchaseOrderItems.Remove(item);
        
        // Recalculate total amount
        var items = await _context.PurchaseOrderItems
            .Where(i => i.PurchaseOrderId == id)
            .ToListAsync();
        
        purchaseOrder!.TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


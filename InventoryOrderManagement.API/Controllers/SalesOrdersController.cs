using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using InventoryOrderManagement.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly InventoryService _inventoryService;

    public SalesOrdersController(AppDbContext context, InventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    // GET: api/salesorders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrders()
    {
        return await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
                .ThenInclude(item => item.Product)
            .ToListAsync();
    }

    // GET: api/salesorders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrder>> GetSalesOrder(int id)
    {
        var salesOrder = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(so => so.SalesOrderId == id);

        if (salesOrder == null)
        {
            return NotFound(new { message = $"Sales Order with ID {id} not found." });
        }

        return salesOrder;
    }

    // GET: api/salesorders/customer/5
    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrdersByCustomer(int customerId)
    {
        var salesOrders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
                .ThenInclude(item => item.Product)
            .Where(so => so.CustomerId == customerId)
            .ToListAsync();

        return salesOrders;
    }

    // GET: api/salesorders/status/Pending
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<SalesOrder>>> GetSalesOrdersByStatus(string status)
    {
        var salesOrders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
                .ThenInclude(item => item.Product)
            .Where(so => so.Status == status)
            .ToListAsync();

        return salesOrders;
    }

    // POST: api/salesorders
    [HttpPost]
    public async Task<ActionResult<SalesOrder>> CreateSalesOrder(CreateSalesOrderDto dto)
    {
        // Validate CustomerId
        var customerExists = await _context.Customers
            .AnyAsync(c => c.CustomerId == dto.CustomerId);
        
        if (!customerExists)
        {
            return BadRequest(new { message = $"Customer with ID {dto.CustomerId} does not exist." });
        }

        // Validate items
        if (dto.Items == null || !dto.Items.Any())
        {
            return BadRequest(new { message = "Sales order must have at least one item." });
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

        // Validate stock availability for all items - prevent negative stock
        foreach (var item in dto.Items)
        {
            try
            {
                await _inventoryService.ValidateStockAvailabilityAsync(item.ProductId, item.Quantity);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = $"Product ID {item.ProductId}: {ex.Message}" });
            }
        }

        // Calculate total amount
        var totalAmount = dto.Items.Sum(item => item.Quantity * item.UnitPrice);

        // Create sales order
        var salesOrder = new SalesOrder
        {
            CustomerId = dto.CustomerId,
            OrderDate = dto.OrderDate ?? DateTime.UtcNow,
            Status = dto.Status,
            TotalAmount = totalAmount
        };

        _context.SalesOrders.Add(salesOrder);
        await _context.SaveChangesAsync();

        // Create sales order items
        var salesOrderItems = dto.Items.Select(item => new SalesOrderItem
        {
            SalesOrderId = salesOrder.SalesOrderId,
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice
        }).ToList();

        _context.SalesOrderItems.AddRange(salesOrderItems);
        await _context.SaveChangesAsync();

        // Load navigation properties for response
        await _context.Entry(salesOrder)
            .Reference(so => so.Customer)
            .LoadAsync();
        await _context.Entry(salesOrder)
            .Collection(so => so.SalesOrderItems)
            .LoadAsync();

        foreach (var item in salesOrderItems)
        {
            await _context.Entry(item)
                .Reference(i => i.Product)
                .LoadAsync();
        }

        return CreatedAtAction(nameof(GetSalesOrder), new { id = salesOrder.SalesOrderId }, salesOrder);
    }

    // PUT: api/salesorders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSalesOrder(int id, UpdateSalesOrderDto dto)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(id);

        if (salesOrder == null)
        {
            return NotFound(new { message = $"Sales Order with ID {id} not found." });
        }

        // Validate CustomerId
        var customerExists = await _context.Customers
            .AnyAsync(c => c.CustomerId == dto.CustomerId);
        
        if (!customerExists)
        {
            return BadRequest(new { message = $"Customer with ID {dto.CustomerId} does not exist." });
        }

        salesOrder.CustomerId = dto.CustomerId;
        salesOrder.OrderDate = dto.OrderDate;
        salesOrder.Status = dto.Status;

        // Recalculate total amount from items
        var items = await _context.SalesOrderItems
            .Where(item => item.SalesOrderId == id)
            .ToListAsync();
        
        salesOrder.TotalAmount = items.Sum(item => item.Quantity * item.UnitPrice);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/salesorders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalesOrder(int id)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(id);
        
        if (salesOrder == null)
        {
            return NotFound(new { message = $"Sales Order with ID {id} not found." });
        }

        // Items will be deleted automatically due to cascade delete
        _context.SalesOrders.Remove(salesOrder);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/salesorders/5/items
    [HttpPost("{id}/items")]
    public async Task<ActionResult<SalesOrderItem>> AddSalesOrderItem(int id, CreateSalesOrderItemDto dto)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(id);
        
        if (salesOrder == null)
        {
            return NotFound(new { message = $"Sales Order with ID {id} not found." });
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
            SalesOrderId = id,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            UnitPrice = dto.UnitPrice
        };

        _context.SalesOrderItems.Add(item);
        
        // Recalculate total amount
        var items = await _context.SalesOrderItems
            .Where(i => i.SalesOrderId == id)
            .ToListAsync();
        
        salesOrder.TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice) + (dto.Quantity * dto.UnitPrice);
        
        await _context.SaveChangesAsync();

        // Load navigation properties
        await _context.Entry(item)
            .Reference(i => i.Product)
            .LoadAsync();

        return CreatedAtAction(nameof(GetSalesOrder), new { id = salesOrder.SalesOrderId }, item);
    }

    // DELETE: api/salesorders/5/items/10
    [HttpDelete("{id}/items/{itemId}")]
    public async Task<IActionResult> DeleteSalesOrderItem(int id, int itemId)
    {
        var item = await _context.SalesOrderItems
            .FirstOrDefaultAsync(i => i.SalesOrderItemId == itemId && i.SalesOrderId == id);
        
        if (item == null)
        {
            return NotFound(new { message = $"Sales Order Item with ID {itemId} not found in Sales Order {id}." });
        }

        var salesOrder = await _context.SalesOrders.FindAsync(id);
        
        _context.SalesOrderItems.Remove(item);
        
        // Recalculate total amount
        var items = await _context.SalesOrderItems
            .Where(i => i.SalesOrderId == id)
            .ToListAsync();
        
        salesOrder!.TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice);
        
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


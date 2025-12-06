using InventoryOrderManagement.API.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Services;

public class InventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the total stock quantity for a product across all warehouses
    /// </summary>
    public async Task<int> GetTotalStockQuantityAsync(int productId)
    {
        return await _context.Inventories
            .Where(i => i.ProductId == productId)
            .SumAsync(i => i.Quantity);
    }

    /// <summary>
    /// Gets the stock quantity for a product in a specific warehouse
    /// </summary>
    public async Task<int> GetStockQuantityByWarehouseAsync(int productId, int warehouseId)
    {
        return await _context.Inventories
            .Where(i => i.ProductId == productId && i.WarehouseId == warehouseId)
            .SumAsync(i => i.Quantity);
    }

    /// <summary>
    /// Validates if there's enough stock for a sale
    /// Throws exception if stock would go negative
    /// </summary>
    public async Task ValidateStockAvailabilityAsync(int productId, int quantity)
    {
        var totalStock = await GetTotalStockQuantityAsync(productId);
        
        if (totalStock - quantity < 0)
        {
            throw new InvalidOperationException($"Insufficient stock. Available: {totalStock}, Requested: {quantity}. Stock cannot be negative.");
        }
    }

    /// <summary>
    /// Validates if there's enough stock for a sale in a specific warehouse
    /// Throws exception if stock would go negative
    /// </summary>
    public async Task ValidateStockAvailabilityByWarehouseAsync(int productId, int warehouseId, int quantity)
    {
        var stock = await GetStockQuantityByWarehouseAsync(productId, warehouseId);
        
        if (stock - quantity < 0)
        {
            throw new InvalidOperationException($"Insufficient stock in warehouse. Available: {stock}, Requested: {quantity}. Stock cannot be negative.");
        }
    }

    /// <summary>
    /// Validates if updating inventory quantity would result in negative stock
    /// Throws exception if stock would go negative
    /// </summary>
    public async Task ValidateInventoryQuantityAsync(int inventoryId, int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new InvalidOperationException($"Stock quantity cannot be negative. Attempted to set quantity to {newQuantity}.");
        }
    }
}


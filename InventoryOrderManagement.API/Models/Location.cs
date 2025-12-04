namespace InventoryOrderManagement.API.Models;

public class Location
{
    public int LocationId { get; set; }
    public int WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    // Navigation properties
    public Warehouse Warehouse { get; set; } = null!;
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}


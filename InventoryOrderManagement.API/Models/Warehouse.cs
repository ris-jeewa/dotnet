namespace InventoryOrderManagement.API.Models;

public class Warehouse
{
    public int WarehouseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    
    // Navigation properties
    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}


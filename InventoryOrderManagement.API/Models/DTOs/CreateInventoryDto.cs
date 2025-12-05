namespace InventoryOrderManagement.API.Models.DTOs;

public class CreateInventoryDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public int Quantity { get; set; } = 0;
    public int ReorderLevel { get; set; } = 5;
}


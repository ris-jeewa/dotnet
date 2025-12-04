namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdateInventoryDto
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int? LocationId { get; set; }
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
}


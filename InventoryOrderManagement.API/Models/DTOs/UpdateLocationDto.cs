namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdateLocationDto
{
    public int WarehouseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}


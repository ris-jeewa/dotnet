namespace InventoryOrderManagement.API.Models.DTOs;

public class CreatePurchaseOrderDto
{
    public int SupplierId { get; set; }
    public DateTime? OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}


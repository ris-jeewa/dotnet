namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdatePurchaseOrderDto
{
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}


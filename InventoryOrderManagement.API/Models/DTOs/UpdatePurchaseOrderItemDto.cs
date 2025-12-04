namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdatePurchaseOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}


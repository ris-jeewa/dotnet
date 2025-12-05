namespace InventoryOrderManagement.API.Models.DTOs;

public class CreatePurchaseOrderItemDto
{
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}


namespace InventoryOrderManagement.API.Models.DTOs;

public class CreateSalesOrderItemDto
{
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}


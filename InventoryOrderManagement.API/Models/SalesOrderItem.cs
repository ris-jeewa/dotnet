namespace InventoryOrderManagement.API.Models;

public class SalesOrderItem
{
    public int SalesOrderItemId { get; set; }
    public int SalesOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    
    // Navigation properties
    public SalesOrder SalesOrder { get; set; } = null!;
    public Product Product { get; set; } = null!;
}


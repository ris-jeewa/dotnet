namespace InventoryOrderManagement.API.Models;

public class SalesOrder
{
    public int SalesOrderId { get; set; }
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; } = 0;
    
    // Navigation properties
    public Customer Customer { get; set; } = null!;
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}


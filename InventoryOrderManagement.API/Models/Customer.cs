namespace InventoryOrderManagement.API.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    // Navigation property
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}


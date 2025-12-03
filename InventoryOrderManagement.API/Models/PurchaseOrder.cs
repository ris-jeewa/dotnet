namespace InventoryOrderManagement.API.Models;

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }
    public int SupplierId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public decimal TotalAmount { get; set; } = 0;
    
    // Navigation properties
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}


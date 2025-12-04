namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdateSalesOrderDto
{
    public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
}


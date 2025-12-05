namespace InventoryOrderManagement.API.Models.DTOs;

public class CreateSalesOrderDto
{
    public int CustomerId { get; set; }
    public DateTime? OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public List<CreateSalesOrderItemDto> Items { get; set; } = new();
}


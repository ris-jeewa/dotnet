namespace InventoryOrderManagement.API.Models.DTOs;

public class UpdateProductDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
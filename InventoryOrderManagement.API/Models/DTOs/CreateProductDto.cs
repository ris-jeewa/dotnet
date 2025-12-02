namespace InventoryOrderManagement.API.Models.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
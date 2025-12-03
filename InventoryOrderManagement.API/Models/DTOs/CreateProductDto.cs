namespace InventoryOrderManagement.API.Models.DTOs;

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
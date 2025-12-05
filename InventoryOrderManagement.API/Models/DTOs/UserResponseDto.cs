namespace InventoryOrderManagement.API.Models.DTOs;

public class UserResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public RoleDto? Role { get; set; }
}

public class RoleDto
{
    public int RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
}


using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public RolesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/roles
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
    {
        return await _context.Roles
            .Include(r => r.Users)
            .ToListAsync();
    }

    // GET: api/roles/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Role>> GetRole(int id)
    {
        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == id);

        if (role == null)
        {
            return NotFound(new { message = $"Role with ID {id} not found." });
        }

        return role;
    }

    // POST: api/roles
    [HttpPost]
    public async Task<ActionResult<Role>> CreateRole(CreateRoleDto dto)
    {
        // Check if role name already exists
        var existingRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == dto.Name.ToLower());
        
        if (existingRole != null)
        {
            return BadRequest(new { message = $"Role with name '{dto.Name}' already exists." });
        }

        var role = new Role
        {
            Name = dto.Name
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, role);
    }

    // PUT: api/roles/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(int id, UpdateRoleDto dto)
    {
        var role = await _context.Roles.FindAsync(id);

        if (role == null)
        {
            return NotFound(new { message = $"Role with ID {id} not found." });
        }

        // Check if role name is being changed and if new name already exists
        if (role.Name.ToLower() != dto.Name.ToLower())
        {
            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name.ToLower() == dto.Name.ToLower() && r.RoleId != id);
            
            if (existingRole != null)
            {
                return BadRequest(new { message = $"Role with name '{dto.Name}' already exists." });
            }
        }

        role.Name = dto.Name;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/roles/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _context.Roles
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.RoleId == id);
        
        if (role == null)
        {
            return NotFound(new { message = $"Role with ID {id} not found." });
        }

        // Check if role has users
        if (role.Users.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete role. It has {role.Users.Count} user(s) associated with it." 
            });
        }

        _context.Roles.Remove(role);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


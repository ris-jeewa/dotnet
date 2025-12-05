using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .ToListAsync();

        return users.Select(u => new UserResponseDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            CreatedAt = u.CreatedAt,
            Role = u.Role != null ? new RoleDto
            {
                RoleId = u.Role.RoleId,
                Name = u.Role.Name
            } : null
        }).ToList();
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        return new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            CreatedAt = user.CreatedAt,
            Role = user.Role != null ? new RoleDto
            {
                RoleId = user.Role.RoleId,
                Name = user.Role.Name
            } : null
        };
    }

    // GET: api/users/email/{email}
    [HttpGet("email/{email}")]
    public async Task<ActionResult<UserResponseDto>> GetUserByEmail(string email)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            return NotFound(new { message = $"User with email '{email}' not found." });
        }

        return new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            CreatedAt = user.CreatedAt,
            Role = user.Role != null ? new RoleDto
            {
                RoleId = user.Role.RoleId,
                Name = user.Role.Name
            } : null
        };
    }

    // GET: api/users/role/5
    [HttpGet("role/{roleId}")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsersByRole(int roleId)
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .Where(u => u.RoleId == roleId)
            .ToListAsync();

        return users.Select(u => new UserResponseDto
        {
            UserId = u.UserId,
            FullName = u.FullName,
            Email = u.Email,
            RoleId = u.RoleId,
            CreatedAt = u.CreatedAt,
            Role = u.Role != null ? new RoleDto
            {
                RoleId = u.Role.RoleId,
                Name = u.Role.Name
            } : null
        }).ToList();
    }

    // POST: api/users
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(CreateUserDto dto)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        
        if (existingUser != null)
        {
            return BadRequest(new { message = $"User with email '{dto.Email}' already exists." });
        }

        // Validate RoleId if provided
        if (dto.RoleId.HasValue)
        {
            var roleExists = await _context.Roles
                .AnyAsync(r => r.RoleId == dto.RoleId.Value);
            
            if (!roleExists)
            {
                return BadRequest(new { message = $"Role with ID {dto.RoleId} does not exist." });
            }
        }

        // Hash password (using SHA256 for simplicity - in production, use BCrypt or similar)
        var passwordHash = HashPassword(dto.Password);

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = passwordHash,
            RoleId = dto.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Load role for response
        await _context.Entry(user)
            .Reference(u => u.Role)
            .LoadAsync();

        var response = new UserResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            CreatedAt = user.CreatedAt,
            Role = user.Role != null ? new RoleDto
            {
                RoleId = user.Role.RoleId,
                Name = user.Role.Name
            } : null
        };

        return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, response);
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        // Check if email is being changed and if new email already exists
        if (user.Email.ToLower() != dto.Email.ToLower())
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower() && u.UserId != id);
            
            if (existingUser != null)
            {
                return BadRequest(new { message = $"User with email '{dto.Email}' already exists." });
            }
        }

        // Validate RoleId if provided
        if (dto.RoleId.HasValue)
        {
            var roleExists = await _context.Roles
                .AnyAsync(r => r.RoleId == dto.RoleId.Value);
            
            if (!roleExists)
            {
                return BadRequest(new { message = $"Role with ID {dto.RoleId} does not exist." });
            }
        }

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.RoleId = dto.RoleId;

        // Update password only if provided
        if (!string.IsNullOrEmpty(dto.Password))
        {
            user.PasswordHash = HashPassword(dto.Password);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        
        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found." });
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Helper method to hash password
    // NOTE: In production, use a proper password hashing library like BCrypt.Net
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}


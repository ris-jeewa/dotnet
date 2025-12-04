using InventoryOrderManagement.API.Data;
using InventoryOrderManagement.API.Models;
using InventoryOrderManagement.API.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/customers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        return await _context.Customers
            .Include(c => c.SalesOrders)
            .ToListAsync();
    }

    // GET: api/customers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.SalesOrders)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        return customer;
    }

    // POST: api/customers
    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, customer);
    }

    // PUT: api/customers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto dto)
    {
        var customer = await _context.Customers.FindAsync(id);

        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        customer.FullName = dto.FullName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.Address = dto.Address;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/customers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        var customer = await _context.Customers
            .Include(c => c.SalesOrders)
            .FirstOrDefaultAsync(c => c.CustomerId == id);
        
        if (customer == null)
        {
            return NotFound(new { message = $"Customer with ID {id} not found." });
        }

        // Check if customer has sales orders
        if (customer.SalesOrders.Any())
        {
            return BadRequest(new { 
                message = $"Cannot delete customer. It has {customer.SalesOrders.Count} sales order(s) associated with it." 
            });
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}


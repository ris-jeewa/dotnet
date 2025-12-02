using Microsoft.EntityFrameworkCore;
using InventoryOrderManagement.API.Models;

namespace InventoryOrderManagement.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}

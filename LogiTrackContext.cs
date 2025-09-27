using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class LogiTrackContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<InventoryItem> inventoryItems { get; set; }

    public DbSet<Order> orders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=logitrack.db");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdentityUserLogin<string>>()
            .HasKey(l => new { l.LoginProvider, l.ProviderKey});
        
        modelBuilder.Entity<IdentityUserRole<string>>()
            .HasKey(r => new { r.RoleId, r.UserId});
        
        modelBuilder.Entity<IdentityUserToken<string>>()
            .HasKey(t => new {t.UserId, t.LoginProvider, t.Name});

        modelBuilder.Entity<Order>()
            .HasMany(o => o.items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationUser : IdentityUser
{

}

public class RegisterModel
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopApplication.Domain.Entities;

namespace ShopApplication.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Cart ---
        modelBuilder.Entity<Cart>(b =>
        {
            b.HasKey(c => c.Id);
            b.Property(c => c.UserId).IsRequired();

            // Use the private backing field for Items
            b.Navigation(c => c.Items)
             .UsePropertyAccessMode(PropertyAccessMode.Field);

            // One Cart -> many CartItems (shadow FK: CartId)
            b.HasMany(c => c.Items)
             .WithOne()
             .HasForeignKey("CartId")
             .OnDelete(DeleteBehavior.Cascade);

            // Optional: enforce one cart per user
            b.HasIndex(c => c.UserId).IsUnique();
        });

        // --- CartItem ---
        modelBuilder.Entity<CartItem>(b =>
        {
            b.HasKey(ci => ci.Id);
            b.Property(ci => ci.Name).IsRequired().HasMaxLength(256);
            b.Property(ci => ci.Price).HasColumnType("decimal(18,2)");
            b.Property(ci => ci.Quantity).IsRequired();

            // Prevent duplicate product lines in same cart
            b.HasIndex("CartId", nameof(CartItem.ProductId)).IsUnique();
        });
    }

}


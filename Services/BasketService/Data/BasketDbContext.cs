using Microsoft.EntityFrameworkCore;
using BasketService.Models;

namespace BasketService.Data;

public class BasketDbContext : DbContext
{
    public BasketDbContext(DbContextOptions<BasketDbContext> options) : base(options)
    {
    }

    public DbSet<BasketItem> BasketItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BasketItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BasketId).IsRequired();
            entity.Property(e => e.ProductId).IsRequired();
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Quantity).IsRequired();
        });
    }
} 
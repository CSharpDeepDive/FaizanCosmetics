using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.SKU).HasMaxLength(50).IsRequired();

        builder.HasIndex(p => p.Barcode).IsUnique();
        builder.HasIndex(p => p.SKU).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.CategoryId);

        builder.Property(p => p.PurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.SellingPrice).HasPrecision(18, 2);
        builder.Property(p => p.WholesalePrice).HasPrecision(18, 2);
        builder.Property(p => p.CurrentStock).HasPrecision(18, 3);
        builder.Property(p => p.MinimumStockLevel).HasPrecision(18, 3);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);

        builder.HasMany(p => p.PriceHistory)
            .WithOne(h => h.Product)
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.InventoryTransactions)
            .WithOne(t => t.Product)
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Batches)
            .WithOne(b => b.Product)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

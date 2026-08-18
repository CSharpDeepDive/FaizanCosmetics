using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.Property(b => b.BatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(b => b.PurchasePrice).HasPrecision(18, 2);
        builder.Property(b => b.Quantity).HasPrecision(18, 3);
        builder.Property(b => b.RemainingQuantity).HasPrecision(18, 3);
        builder.HasIndex(b => new { b.ProductId, b.BatchNumber }).IsUnique();
        builder.HasIndex(b => b.ExpiryDate);
    }
}

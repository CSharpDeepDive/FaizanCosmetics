using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ProductPriceHistoryConfiguration : IEntityTypeConfiguration<ProductPriceHistory>
{
    public void Configure(EntityTypeBuilder<ProductPriceHistory> builder)
    {
        builder.Property(h => h.OldPurchasePrice).HasPrecision(18, 2);
        builder.Property(h => h.NewPurchasePrice).HasPrecision(18, 2);
        builder.Property(h => h.OldSellingPrice).HasPrecision(18, 2);
        builder.Property(h => h.NewSellingPrice).HasPrecision(18, 2);
        builder.Property(h => h.OldWholesalePrice).HasPrecision(18, 2);
        builder.Property(h => h.NewWholesalePrice).HasPrecision(18, 2);
        builder.HasIndex(h => h.ProductId);

        builder.HasOne(h => h.ChangedByUser)
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

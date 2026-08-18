using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.Property(i => i.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.BarcodeSnapshot).HasMaxLength(50);
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.QuantityReturned).HasPrecision(18, 3);

        foreach (var money in new[] { nameof(SalesInvoiceItem.UnitPrice), nameof(SalesInvoiceItem.UnitCostSnapshot), nameof(SalesInvoiceItem.DiscountAmount), nameof(SalesInvoiceItem.TaxAmount), nameof(SalesInvoiceItem.LineTotal) })
        {
            builder.Property(money).HasPrecision(18, 2);
        }
        builder.Property(i => i.DiscountPercent).HasPrecision(5, 2);
        builder.Property(i => i.TaxPercent).HasPrecision(5, 2);

        builder.HasIndex(i => i.ProductId);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Batch)
            .WithMany()
            .HasForeignKey(i => i.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

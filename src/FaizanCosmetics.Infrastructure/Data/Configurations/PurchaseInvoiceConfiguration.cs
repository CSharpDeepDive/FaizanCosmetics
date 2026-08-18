using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.SupplierId);
        builder.HasIndex(i => i.InvoiceDate);

        foreach (var money in new[] { nameof(PurchaseInvoice.SubTotal), nameof(PurchaseInvoice.DiscountAmount), nameof(PurchaseInvoice.TaxAmount), nameof(PurchaseInvoice.GrandTotal), nameof(PurchaseInvoice.PaidAmount), nameof(PurchaseInvoice.DueAmount) })
        {
            builder.Property(money).HasPrecision(18, 2);
        }

        builder.HasOne(i => i.CreatedByUser).WithMany().HasForeignKey(i => i.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(it => it.PurchaseInvoice)
            .HasForeignKey(it => it.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseInvoiceItemConfiguration : IEntityTypeConfiguration<PurchaseInvoiceItem>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoiceItem> builder)
    {
        builder.Property(i => i.ProductNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.QuantityReturned).HasPrecision(18, 3);
        builder.Property(i => i.UnitCost).HasPrecision(18, 2);
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2);
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.Product).WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(i => i.Batch).WithMany().HasForeignKey(i => i.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

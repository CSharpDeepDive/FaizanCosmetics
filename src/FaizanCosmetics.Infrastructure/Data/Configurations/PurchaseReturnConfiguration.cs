using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.Property(r => r.ReturnNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.ReturnNumber).IsUnique();
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(r => r.PurchaseInvoice)
            .WithMany(i => i.Returns)
            .HasForeignKey(r => r.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser).WithMany().HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne(it => it.PurchaseReturn)
            .HasForeignKey(it => it.PurchaseReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PurchaseReturnItemConfiguration : IEntityTypeConfiguration<PurchaseReturnItem>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnItem> builder)
    {
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitCost).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.PurchaseInvoiceItem).WithMany().HasForeignKey(i => i.PurchaseInvoiceItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

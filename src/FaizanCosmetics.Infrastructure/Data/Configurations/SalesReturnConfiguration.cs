using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.Property(r => r.ReturnNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(r => r.ReturnNumber).IsUnique();
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);

        builder.HasOne(r => r.SalesInvoice)
            .WithMany(i => i.Returns)
            .HasForeignKey(r => r.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser).WithMany().HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Items)
            .WithOne(it => it.SalesReturn)
            .HasForeignKey(it => it.SalesReturnId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SalesReturnItemConfiguration : IEntityTypeConfiguration<SalesReturnItem>
{
    public void Configure(EntityTypeBuilder<SalesReturnItem> builder)
    {
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.LineTotal).HasPrecision(18, 2);

        builder.HasOne(i => i.SalesInvoiceItem)
            .WithMany()
            .HasForeignKey(i => i.SalesInvoiceItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

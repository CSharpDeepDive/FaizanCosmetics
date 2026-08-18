using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.Property(i => i.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.InvoiceDate);
        builder.HasIndex(i => i.ClientId);
        builder.HasIndex(i => i.Status);

        foreach (var money in new[] { nameof(SalesInvoice.SubTotal), nameof(SalesInvoice.DiscountAmount), nameof(SalesInvoice.TaxAmount), nameof(SalesInvoice.GrandTotal), nameof(SalesInvoice.PaidAmount), nameof(SalesInvoice.DueAmount) })
        {
            builder.Property(money).HasPrecision(18, 2);
        }

        builder.HasOne(i => i.CreatedByUser)
            .WithMany(u => u.SalesInvoices)
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Client)
            .WithMany(c => c.SalesInvoices)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Items)
            .WithOne(it => it.SalesInvoice)
            .HasForeignKey(it => it.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade); // invoice items are owned by their invoice
    }
}

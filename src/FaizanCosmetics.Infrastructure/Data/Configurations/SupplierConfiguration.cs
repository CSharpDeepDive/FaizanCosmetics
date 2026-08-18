using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(150).IsRequired();
        builder.Property(s => s.OpeningBalance).HasPrecision(18, 2);
        builder.HasIndex(s => s.Name);

        builder.HasMany(s => s.Products)
            .WithOne(p => p.Supplier)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.PurchaseInvoices)
            .WithOne(p => p.Supplier)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.LedgerEntries)
            .WithOne(l => l.Supplier)
            .HasForeignKey(l => l.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

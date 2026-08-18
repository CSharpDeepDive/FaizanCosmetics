using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class SupplierPaymentConfiguration : IEntityTypeConfiguration<SupplierPayment>
{
    public void Configure(EntityTypeBuilder<SupplierPayment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.PaidByUser).WithMany().HasForeignKey(p => p.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Supplier).WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Allocations)
            .WithOne(a => a.SupplierPayment)
            .HasForeignKey(a => a.SupplierPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupplierPaymentAllocationConfiguration : IEntityTypeConfiguration<SupplierPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<SupplierPaymentAllocation> builder)
    {
        builder.Property(a => a.AllocatedAmount).HasPrecision(18, 2);
        builder.HasOne(a => a.PurchaseInvoice)
            .WithMany(i => i.PaymentAllocations)
            .HasForeignKey(a => a.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SupplierLedgerEntryConfiguration : IEntityTypeConfiguration<SupplierLedgerEntry>
{
    public void Configure(EntityTypeBuilder<SupplierLedgerEntry> builder)
    {
        builder.Property(l => l.Debit).HasPrecision(18, 2);
        builder.Property(l => l.Credit).HasPrecision(18, 2);
        builder.Property(l => l.Balance).HasPrecision(18, 2);

        builder.HasIndex(l => l.SupplierId);
        builder.HasIndex(l => new { l.ReferenceType, l.ReferenceId });

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

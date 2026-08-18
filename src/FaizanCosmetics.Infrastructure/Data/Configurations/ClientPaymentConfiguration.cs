using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ClientPaymentConfiguration : IEntityTypeConfiguration<ClientPayment>
{
    public void Configure(EntityTypeBuilder<ClientPayment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.HasIndex(p => p.ClientId);
        builder.HasIndex(p => p.PaymentDate);

        builder.HasOne(p => p.ReceivedByUser).WithMany().HasForeignKey(p => p.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Client).WithMany().HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Allocations)
            .WithOne(a => a.ClientPayment)
            .HasForeignKey(a => a.ClientPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ClientPaymentAllocationConfiguration : IEntityTypeConfiguration<ClientPaymentAllocation>
{
    public void Configure(EntityTypeBuilder<ClientPaymentAllocation> builder)
    {
        builder.Property(a => a.AllocatedAmount).HasPrecision(18, 2);
        builder.HasOne(a => a.SalesInvoice)
            .WithMany(i => i.PaymentAllocations)
            .HasForeignKey(a => a.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

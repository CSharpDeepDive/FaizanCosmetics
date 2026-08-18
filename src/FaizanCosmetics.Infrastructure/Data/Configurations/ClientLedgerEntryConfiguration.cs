using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ClientLedgerEntryConfiguration : IEntityTypeConfiguration<ClientLedgerEntry>
{
    public void Configure(EntityTypeBuilder<ClientLedgerEntry> builder)
    {
        builder.Property(l => l.Debit).HasPrecision(18, 2);
        builder.Property(l => l.Credit).HasPrecision(18, 2);
        builder.Property(l => l.Balance).HasPrecision(18, 2);

        builder.HasIndex(l => l.ClientId);
        builder.HasIndex(l => new { l.ReferenceType, l.ReferenceId });
        builder.HasIndex(l => l.EntryDate);

        builder.HasOne(l => l.User).WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

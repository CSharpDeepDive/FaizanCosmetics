using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasIndex(c => c.ClientCode).IsUnique();
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Phone);
        builder.Property(c => c.ClientCode).HasMaxLength(20).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(150).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);
        builder.Property(c => c.OpeningBalance).HasPrecision(18, 2);

        builder.HasMany(c => c.SalesInvoices)
            .WithOne(i => i.Client)
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.LedgerEntries)
            .WithOne(l => l.Client)
            .HasForeignKey(l => l.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

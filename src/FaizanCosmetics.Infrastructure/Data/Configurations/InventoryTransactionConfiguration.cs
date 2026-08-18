using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.Property(t => t.Quantity).HasPrecision(18, 3);
        builder.Property(t => t.PreviousStock).HasPrecision(18, 3);
        builder.Property(t => t.NewStock).HasPrecision(18, 3);
        builder.Property(t => t.UnitCost).HasPrecision(18, 2);

        builder.HasIndex(t => t.ProductId);
        builder.HasIndex(t => new { t.ReferenceType, t.ReferenceId });
        builder.HasIndex(t => t.TransactionDate);

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Batch)
            .WithMany(b => b.InventoryTransactions)
            .HasForeignKey(t => t.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

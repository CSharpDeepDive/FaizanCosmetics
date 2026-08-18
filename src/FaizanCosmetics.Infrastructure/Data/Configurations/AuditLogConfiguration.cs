using FaizanCosmetics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FaizanCosmetics.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Entity).HasMaxLength(100).IsRequired();
        builder.HasIndex(a => a.DateTime);
        builder.HasIndex(a => new { a.Entity, a.EntityId });

        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.Property(s => s.DefaultDiscountPercent).HasPrecision(5, 2);
        builder.Property(s => s.DefaultTaxPercent).HasPrecision(5, 2);
    }
}

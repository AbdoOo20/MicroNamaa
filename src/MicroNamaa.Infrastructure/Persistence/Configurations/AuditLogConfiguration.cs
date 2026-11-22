using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Action).HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.EntityName).HasMaxLength(100).IsUnicode(false);

        builder.Property(x => x.EntityId).HasMaxLength(100).IsUnicode(false);

        builder.Property(x => x.IpAddress).HasMaxLength(64).IsUnicode(false);

        builder.HasIndex(x => new { x.EntityName, x.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity");

        builder.HasIndex(x => new { x.MerchantId, x.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Merchant");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");
    }
}

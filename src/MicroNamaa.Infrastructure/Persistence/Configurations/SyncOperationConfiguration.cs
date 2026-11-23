using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SyncOperationConfiguration : IEntityTypeConfiguration<SyncOperation>
{
    public void Configure(EntityTypeBuilder<SyncOperation> builder)
    {
        builder.ToTable("SyncOperations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(50).IsUnicode(false);

        builder.Property(x => x.ClientKey).HasMaxLength(100);

        builder.Property(x => x.EntityType).HasMaxLength(40).IsUnicode(false);

        builder.Property(x => x.Code).HasMaxLength(40);

        builder.Property(x => x.Message).HasMaxLength(500);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.OperationId })
            .HasDatabaseName("UX_SyncOperations_Operation")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.ClientKey })
            .HasDatabaseName("IX_SyncOperations_ClientKey")
            .HasFilter("[ClientKey] IS NOT NULL");
    }
}

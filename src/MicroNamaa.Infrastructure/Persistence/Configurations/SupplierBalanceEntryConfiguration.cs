using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SupplierBalanceEntryConfiguration : IEntityTypeConfiguration<SupplierBalanceEntry>
{
    public void Configure(EntityTypeBuilder<SupplierBalanceEntry> builder)
    {
        builder.ToTable("SupplierBalanceEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 3);

        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);

        builder.Property(x => x.Reference).HasMaxLength(100);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.SupplierId, x.CreatedAt })
            .HasDatabaseName("IX_SupplierBalanceEntries_Supplier");

        builder.HasIndex(x => new { x.MerchantId, x.Type })
            .HasDatabaseName("IX_SupplierBalanceEntries_Merchant");
    }
}

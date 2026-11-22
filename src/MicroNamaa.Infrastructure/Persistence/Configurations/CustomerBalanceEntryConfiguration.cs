using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class CustomerBalanceEntryConfiguration : IEntityTypeConfiguration<CustomerBalanceEntry>
{
    public void Configure(EntityTypeBuilder<CustomerBalanceEntry> builder)
    {
        builder.ToTable("CustomerBalanceEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 3);

        builder.Property(x => x.BalanceAfter).HasPrecision(18, 3);

        builder.Property(x => x.Reference).HasMaxLength(100);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt })
            .HasDatabaseName("IX_CustomerBalanceEntries_Customer");

        builder.HasIndex(x => x.MerchantId)
            .HasDatabaseName("IX_CustomerBalanceEntries_Merchant");
    }
}

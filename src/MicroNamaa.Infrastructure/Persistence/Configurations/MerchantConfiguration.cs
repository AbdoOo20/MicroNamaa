using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("Merchants");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number).HasDefaultValueSql("NEXT VALUE FOR [MerchantNumbers]");

        builder.Property(x => x.Name).HasMaxLength(150);

        builder.Property(x => x.OwnerName).HasMaxLength(150);

        builder.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.Email).HasMaxLength(256);

        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.Address).HasMaxLength(300);

        builder.Property(x => x.TaxNumber).HasMaxLength(50);

        builder.Property(x => x.LogoPath).HasMaxLength(300);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.TaxRate).HasPrecision(5, 2);

        builder.Property(x => x.PointsPerCurrencyUnit).HasPrecision(10, 3);

        builder.Property(x => x.PointValue).HasPrecision(10, 3);

        builder.Property(x => x.ReceiptFooter).HasMaxLength(500);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Ignore(x => x.Code);

        builder.HasIndex(x => x.Number)
            .HasDatabaseName("UX_Merchants_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.SubscriptionEndsAt })
            .HasDatabaseName("IX_Merchants_Status");
    }
}

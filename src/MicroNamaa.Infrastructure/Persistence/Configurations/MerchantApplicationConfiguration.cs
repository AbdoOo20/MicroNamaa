using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class MerchantApplicationConfiguration : IEntityTypeConfiguration<MerchantApplication>
{
    public void Configure(EntityTypeBuilder<MerchantApplication> builder)
    {
        builder.ToTable("MerchantApplications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessName).HasMaxLength(150);

        builder.Property(x => x.OwnerName).HasMaxLength(150);

        builder.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.Email).HasMaxLength(256);

        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.BusinessType).HasMaxLength(100);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.AdminNote).HasMaxLength(500);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("IX_MerchantApplications_Status");

        builder.HasIndex(x => x.Phone)
            .HasDatabaseName("IX_MerchantApplications_Phone");
    }
}

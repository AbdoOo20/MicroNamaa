using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150);

        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.Address).HasMaxLength(300);

        builder.Property(x => x.Phone).HasMaxLength(20);

        builder.HasOne(x => x.Merchant)
            .WithMany(x => x.Branches)
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DefaultWarehouse)
            .WithMany()
            .HasForeignKey(x => x.DefaultWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MerchantId)
            .HasDatabaseName("IX_Branches_Merchant");
    }
}

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150);

        builder.Property(x => x.Phone).HasMaxLength(20).IsUnicode(false);

        builder.Property(x => x.Email).HasMaxLength(256);

        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.Address).HasMaxLength(300);

        builder.Property(x => x.AvatarPath).HasMaxLength(300);

        builder.Property(x => x.Balance).HasPrecision(18, 3);

        builder.Property(x => x.CreditLimit).HasPrecision(18, 3);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Ignore(x => x.Code);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Number })
            .HasDatabaseName("UX_Customers_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.Phone })
            .HasDatabaseName("UX_Customers_Phone")
            .IsUnique()
            .HasFilter("[Phone] IS NOT NULL AND [IsDeleted] = 0");
    }
}

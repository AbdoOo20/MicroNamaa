using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150);

        builder.Property(x => x.ContactPerson).HasMaxLength(150);

        builder.Property(x => x.Phone).HasMaxLength(20);

        builder.Property(x => x.Email).HasMaxLength(256);

        builder.Property(x => x.City).HasMaxLength(100);

        builder.Property(x => x.Address).HasMaxLength(300);

        builder.Property(x => x.TaxNumber).HasMaxLength(50);

        builder.Property(x => x.LogoPath).HasMaxLength(300);

        builder.Property(x => x.Balance).HasPrecision(18, 3);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Ignore(x => x.Code);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Number })
            .HasDatabaseName("UX_Suppliers_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.Name })
            .HasDatabaseName("UX_Suppliers_Name")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200);

        builder.Property(x => x.Sku).HasMaxLength(50);

        builder.Property(x => x.Barcode).HasMaxLength(50);

        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.ImagePath).HasMaxLength(300);

        builder.Property(x => x.Cost).HasPrecision(18, 3);

        builder.Property(x => x.Price).HasPrecision(18, 3);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Sku })
            .HasDatabaseName("UX_Products_Sku")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.MerchantId, x.Barcode })
            .HasDatabaseName("UX_Products_Barcode")
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.MerchantId, x.Status, x.CategoryId })
            .HasDatabaseName("IX_Products_Status");
    }
}

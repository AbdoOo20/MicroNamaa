using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100);

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.ImagePath).HasMaxLength(300);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.HasOne<Merchant>()
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Name })
            .HasDatabaseName("UX_Categories_Name")
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}

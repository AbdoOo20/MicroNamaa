using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SalesInvoiceItemConfiguration : IEntityTypeConfiguration<SalesInvoiceItem>
{
    public void Configure(EntityTypeBuilder<SalesInvoiceItem> builder)
    {
        builder.ToTable("SalesInvoiceItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductName).HasMaxLength(200);

        builder.Property(x => x.Sku).HasMaxLength(50);

        builder.Property(x => x.UnitPrice).HasPrecision(18, 3);

        builder.Property(x => x.UnitCost).HasPrecision(18, 3);

        builder.Property(x => x.DiscountAmount).HasPrecision(18, 3);

        builder.Property(x => x.PromotionName).HasMaxLength(100);

        builder.Property(x => x.LineTotal).HasPrecision(18, 3);

        builder.HasOne(x => x.SalesInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.ProductId })
            .HasDatabaseName("IX_SalesInvoiceItems_Product");
    }
}

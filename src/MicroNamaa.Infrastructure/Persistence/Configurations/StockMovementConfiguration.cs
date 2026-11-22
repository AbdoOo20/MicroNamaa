using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UnitCost).HasPrecision(18, 3);

        builder.Property(x => x.ReferenceType).HasMaxLength(40).IsUnicode(false);

        builder.Property(x => x.ReferenceCode).HasMaxLength(40);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.ProductId, x.CreatedAt })
            .HasDatabaseName("IX_StockMovements_Product");

        builder.HasIndex(x => new { x.MerchantId, x.WarehouseId, x.CreatedAt })
            .HasDatabaseName("IX_StockMovements_Warehouse");
    }
}

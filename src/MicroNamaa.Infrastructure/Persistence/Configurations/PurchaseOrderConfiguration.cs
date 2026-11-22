using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentTerms).HasMaxLength(50);

        builder.Property(x => x.Subtotal).HasPrecision(18, 3);

        builder.Property(x => x.Discount).HasPrecision(18, 3);

        builder.Property(x => x.TaxRate).HasPrecision(5, 2);

        builder.Property(x => x.TaxAmount).HasPrecision(18, 3);

        builder.Property(x => x.ShippingCost).HasPrecision(18, 3);

        builder.Property(x => x.Total).HasPrecision(18, 3);

        builder.Property(x => x.PaidAmount).HasPrecision(18, 3);

        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.Property(x => x.CancelReason).HasMaxLength(500);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Ignore(x => x.Code);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Number })
            .HasDatabaseName("UX_PurchaseOrders_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.Status, x.OrderDate })
            .HasDatabaseName("IX_PurchaseOrders_Status");

        builder.HasIndex(x => new { x.SupplierId, x.Status })
            .HasDatabaseName("IX_PurchaseOrders_Supplier");
    }
}

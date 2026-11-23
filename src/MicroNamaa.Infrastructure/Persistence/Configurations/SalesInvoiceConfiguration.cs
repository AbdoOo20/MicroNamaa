using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ToTable("SalesInvoices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subtotal).HasPrecision(18, 3);

        builder.Property(x => x.ItemsDiscount).HasPrecision(18, 3);

        builder.Property(x => x.PromotionName).HasMaxLength(100);

        builder.Property(x => x.ManualDiscountValue).HasPrecision(18, 3);

        builder.Property(x => x.OrderDiscount).HasPrecision(18, 3);

        builder.Property(x => x.PointsDiscount).HasPrecision(18, 3);

        builder.Property(x => x.TaxRate).HasPrecision(5, 2);

        builder.Property(x => x.TaxAmount).HasPrecision(18, 3);

        builder.Property(x => x.Total).HasPrecision(18, 3);

        builder.Property(x => x.PaidAmount).HasPrecision(18, 3);

        builder.Property(x => x.TenderedAmount).HasPrecision(18, 3);

        builder.Property(x => x.ChangeAmount).HasPrecision(18, 3);

        builder.Property(x => x.DueAmount).HasPrecision(18, 3);

        builder.Property(x => x.TotalCost).HasPrecision(18, 3);

        builder.Property(x => x.ReturnedAmount).HasPrecision(18, 3);

        builder.Property(x => x.Note).HasMaxLength(500);

        builder.Property(x => x.CancelReason).HasMaxLength(500);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.Ignore(x => x.Code);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Register)
            .WithMany()
            .HasForeignKey(x => x.RegisterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Cashier)
            .WithMany()
            .HasForeignKey(x => x.CashierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Number })
            .HasDatabaseName("UX_SalesInvoices_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.Status, x.CompletedAt })
            .HasDatabaseName("IX_SalesInvoices_Status_Completed");

        builder.HasIndex(x => new { x.MerchantId, x.BranchId, x.CompletedAt })
            .HasDatabaseName("IX_SalesInvoices_Branch");

        builder.HasIndex(x => new { x.CustomerId, x.Status })
            .HasDatabaseName("IX_SalesInvoices_Customer");

        builder.HasIndex(x => new { x.CashierId, x.CompletedAt })
            .HasDatabaseName("IX_SalesInvoices_Cashier");
    }
}

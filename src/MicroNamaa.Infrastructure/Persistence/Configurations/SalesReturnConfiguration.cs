using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ToTable("SalesReturns");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Total).HasPrecision(18, 3);

        builder.Property(x => x.TotalCost).HasPrecision(18, 3);

        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.Ignore(x => x.Code);

        builder.HasOne(x => x.SalesInvoice)
            .WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Customer)
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.MerchantId, x.Number })
            .HasDatabaseName("UX_SalesReturns_Number")
            .IsUnique();

        builder.HasIndex(x => new { x.MerchantId, x.CreatedAt })
            .HasDatabaseName("IX_SalesReturns_CreatedAt");

        builder.HasIndex(x => x.SalesInvoiceId)
            .HasDatabaseName("IX_SalesReturns_Invoice");
    }
}

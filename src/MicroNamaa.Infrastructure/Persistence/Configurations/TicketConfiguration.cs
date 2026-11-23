using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject).HasMaxLength(200);

        builder.HasOne(x => x.OpenedBy)
            .WithMany()
            .HasForeignKey(x => x.OpenedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Merchant)
            .WithMany()
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.Status, x.LastMessageAt })
            .HasDatabaseName("IX_Tickets_Status");

        builder.HasIndex(x => x.MerchantId)
            .HasDatabaseName("IX_Tickets_Merchant");
    }
}

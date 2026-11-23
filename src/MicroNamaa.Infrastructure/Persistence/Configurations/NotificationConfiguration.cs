using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasMaxLength(40).IsUnicode(false);

        builder.Property(x => x.Title).HasMaxLength(200);

        builder.Property(x => x.Body).HasMaxLength(1000);

        builder.Property(x => x.RelatedType).HasMaxLength(40).IsUnicode(false);

        builder.HasIndex(x => new { x.Audience, x.UserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_User")
            .IsDescending(false, false, false, true);

        builder.HasIndex(x => new { x.Audience, x.MerchantId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_Merchant")
            .IsDescending(false, false, false, true);
    }
}

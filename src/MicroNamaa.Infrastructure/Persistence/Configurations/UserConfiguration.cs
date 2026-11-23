using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(x => x.FullName).HasMaxLength(150);

        builder.Property(x => x.JobTitle).HasMaxLength(100);

        builder.Property(x => x.MonthlyTarget).HasPrecision(18, 3);

        builder.Property(x => x.AvatarPath).HasMaxLength(300);

        builder.Property(x => x.SearchText).HasMaxLength(600);

        builder.HasOne(x => x.Merchant)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.MerchantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.NormalizedUserName)
            .HasDatabaseName("UserNameIndex")
            .IsUnique()
            .HasFilter("[NormalizedUserName] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => x.NormalizedEmail)
            .HasDatabaseName("EmailIndex")
            .IsUnique()
            .HasFilter("[NormalizedEmail] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => x.PhoneNumber)
            .HasDatabaseName("UX_Users_Phone")
            .IsUnique()
            .HasFilter("[PhoneNumber] IS NOT NULL AND [IsDeleted] = 0");

        builder.HasIndex(x => new { x.MerchantId, x.BranchId })
            .HasDatabaseName("IX_Users_Merchant");
    }
}

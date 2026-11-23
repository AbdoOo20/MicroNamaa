using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class StaffPermissionConfiguration : IEntityTypeConfiguration<StaffPermission>
{
    public void Configure(EntityTypeBuilder<StaffPermission> builder)
    {
        builder.ToTable("StaffPermissions");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithMany(x => x.StaffPermissions)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.UserId, x.Area })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_StaffPermissions_User_Area");
    }
}

using Microsoft.EntityFrameworkCore;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasData(
            new Role { Id = 1, Name = RoleNames.PlatformAdmin, NormalizedName = "PLATFORMADMIN", ConcurrencyStamp = "5e0c3a1f-9d2b-4c7e-8a41-6b2f0d9e0001" },

            new Role { Id = 2, Name = RoleNames.MerchantOwner, NormalizedName = "MERCHANTOWNER", ConcurrencyStamp = "5e0c3a1f-9d2b-4c7e-8a41-6b2f0d9e0002" },

            new Role { Id = 3, Name = RoleNames.MerchantManager, NormalizedName = "MERCHANTMANAGER", ConcurrencyStamp = "5e0c3a1f-9d2b-4c7e-8a41-6b2f0d9e0003" },

            new Role { Id = 4, Name = RoleNames.Cashier, NormalizedName = "CASHIER", ConcurrencyStamp = "5e0c3a1f-9d2b-4c7e-8a41-6b2f0d9e0004" });
    }
}

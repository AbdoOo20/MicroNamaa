using Microsoft.AspNetCore.Identity;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Infrastructure.Seeding;

public static class PlatformAdminSeeder
{
    public static async Task SeedPlatformAdminAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var settings = scope.ServiceProvider.GetRequiredService<IOptions<PlatformAdminSettings>>().Value;

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PlatformAdminSeeder");

        try
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var email = settings.Email.Trim();

            var existing = await userManager.FindByEmailAsync(email);

            if (existing is not null)
            {
                if (existing.MerchantId is not null)
                {
                    logger.LogWarning("The configured platform admin email belongs to a merchant. Use a different email.");

                    return;
                }

                if (!await userManager.IsInRoleAsync(existing, RoleNames.PlatformAdmin))
                {
                    await userManager.AddToRoleAsync(existing, RoleNames.PlatformAdmin);
                }

                return;
            }

            var user = new User
            {
                UserName = email,

                Email = email,

                EmailConfirmed = true,

                FullName = settings.FullName
            };

            var created = await userManager.CreateAsync(user, settings.Password);

            if (!created.Succeeded)
            {
                logger.LogWarning("Platform admin could not be created: {Errors}", string.Join("; ", created.Errors.Select(error => error.Description)));

                return;
            }

            await userManager.AddToRoleAsync(user, RoleNames.PlatformAdmin);

            logger.LogInformation("Platform admin {Email} was created.", email);
        }

        catch (Exception exception)
        {
            logger.LogWarning(exception, "Platform admin seeding was skipped. Make sure the database is migrated.");
        }
    }
}

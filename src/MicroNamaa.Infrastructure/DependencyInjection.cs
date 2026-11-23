using Microsoft.EntityFrameworkCore.Diagnostics;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Application.Services.Auth;

using MicroNamaa.Application.Services.Employees;

using MicroNamaa.Application.Services.Staff;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Infrastructure.Persistence.Repositories;

using MicroNamaa.Infrastructure.Persistence;

using MicroNamaa.Infrastructure.Services.Auth;

using MicroNamaa.Infrastructure.Services.Employees;

using MicroNamaa.Infrastructure.Services.Merchants;

using MicroNamaa.Infrastructure.Services.Numbers;

using MicroNamaa.Infrastructure.Services.Staff;

using MicroNamaa.Infrastructure.Services.Storage;

namespace MicroNamaa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlServer(connectionString)
            .ConfigureWarnings(warnings => warnings
                .Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
                // Services roll back their own transactions, so EF's automatic savepoints are not needed.
                .Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)));

        services.AddIdentityCore<User>(options =>
        {
            options.User.RequireUniqueEmail = false;

            options.Password.RequiredLength = 8;

            options.Password.RequireNonAlphanumeric = false;

            options.Password.RequireUppercase = false;

            options.Lockout.MaxFailedAccessAttempts = 5;
        })
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddMemoryCache();

        services.AddSingleton<IMerchantAccessService, MerchantAccessService>();

        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.AddScoped<IDocumentNumberService, DocumentNumberService>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<IStaffService, StaffService>();

        services.AddScoped<IEmployeeService, EmployeeService>();

        services.AddScoped<IMerchantProvisioningService, MerchantProvisioningService>();

        return services;
    }
}

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Infrastructure.Persistence;

using MicroNamaa.Infrastructure.Services.Auth;

namespace MicroNamaa.Infrastructure.Services.Merchants;

public class MerchantProvisioningService(UserManager<User> userManager, AppDbContext context, IOptions<PlatformSettings> platformOptions) : IMerchantProvisioningService
{
    public async Task<Result<int>> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default)
    {
        var phone = LibyanPhone.Normalize(request.Phone);

        if (phone is null)
        {
            return Result<int>.BadRequest(LibyanPhone.InvalidMessage);
        }

        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (await userManager.Users.AnyAsync(user => user.PhoneNumber == phone || user.UserName == phone, cancellationToken))
        {
            return Result<int>.Conflict("This mobile number is already registered.");
        }

        if (email is not null && await userManager.FindByEmailAsync(email) is not null)
        {
            return Result<int>.Conflict("This email is already registered.");
        }

        var endsOn = request.SubscriptionEndsOn ?? DateOnly.FromDateTime(LibyaTime.Now).AddDays(platformOptions.Value.DefaultSubscriptionDays);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var merchant = new Merchant
        {
            Name = request.Name.Trim(),

            OwnerName = request.OwnerName.Trim(),

            Phone = phone,

            Email = email,

            City = Clean(request.City),

            Address = Clean(request.Address),

            TaxNumber = Clean(request.TaxNumber),

            SubscriptionEndsAt = AdminMerchantService.EndOfDay(endsOn),

            MaxBranches = request.MaxBranches,

            MaxUsers = request.MaxUsers,

            TaxRate = request.TaxRate,

            Notes = Clean(request.Notes),

            PointsPerCurrencyUnit = 1,

            PointValue = 0.05m,

            MinPointsToRedeem = 100
        };

        context.Merchants.Add(merchant);

        await context.SaveChangesAsync(cancellationToken);

        var branch = new Branch { MerchantId = merchant.Id, Name = string.IsNullOrWhiteSpace(request.BranchName) ? "الفرع الرئيسي" : request.BranchName.Trim(), City = merchant.City, Address = merchant.Address, Phone = phone };

        context.Branches.Add(branch);

        await context.SaveChangesAsync(cancellationToken);

        var warehouse = new Warehouse { MerchantId = merchant.Id, BranchId = branch.Id, Name = "المخزن الرئيسي", Location = merchant.Address ?? merchant.City };

        context.Warehouses.Add(warehouse);

        context.Registers.Add(new Register { MerchantId = merchant.Id, BranchId = branch.Id, Name = "Reg 01" });

        await context.SaveChangesAsync(cancellationToken);

        branch.DefaultWarehouseId = warehouse.Id;

        await context.SaveChangesAsync(cancellationToken);

        var owner = new User
        {
            UserName = phone,

            PhoneNumber = phone,

            Email = email,

            EmailConfirmed = email is not null,

            FullName = merchant.OwnerName,

            MerchantId = merchant.Id,

            BranchId = branch.Id,

            JobTitle = "Owner"
        };

        var created = await userManager.CreateAsync(owner, request.OwnerPassword);

        if (!created.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);

            return Result<int>.BadRequest(created.Errors.Select(error => error.Description));
        }

        await userManager.AddToRoleAsync(owner, RoleNames.MerchantOwner);

        await transaction.CommitAsync(cancellationToken);

        return Result<int>.Created(merchant.Id, "Merchant created.");
    }

    public async Task<Result> ResetPasswordAsync(int merchantId, int userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Id == userId && item.MerchantId == merchantId, cancellationToken);

        if (user is null)
        {
            return Result.NotFound("User was not found.");
        }

        var reset = await PasswordReset.SetAsync(userManager, user, newPassword);

        if (reset.Succeeded)
        {
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        return reset.Succeeded ? Result.Ok("Password changed.") : Result.BadRequest(reset.Errors.Select(error => error.Description));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

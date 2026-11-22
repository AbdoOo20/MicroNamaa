using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Employees;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Services.Employees;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

using MicroNamaa.Infrastructure.Persistence;

using MicroNamaa.Infrastructure.Services.Auth;

namespace MicroNamaa.Infrastructure.Services.Employees;

public class EmployeeService(UserManager<User> userManager, AppDbContext context, IUserAppService userAppService, ImageUploads images) : IEmployeeService
{
    private const string NotFound = "Employee was not found.";

    private static readonly string[] MerchantRoles = [RoleNames.MerchantOwner, RoleNames.MerchantManager, RoleNames.Cashier];

    /// <summary>A user with their role, built with an object initializer so EF can keep filtering and sorting on it.</summary>
    private sealed class Row
    {
        public User User { get; init; } = null!;

        public string Role { get; init; } = string.Empty;
    }

    public async Task<Result<PagedResult<EmployeeDto>>> GetAllAsync(EmployeeQuery query, CancellationToken cancellationToken = default)
    {
        var rows = Filter(query);

        var page = await rows.OrderBy(row => row.User.FullName).ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);

        var items = await ToDtosAsync(page.Items, cancellationToken);

        return Result<PagedResult<EmployeeDto>>.Ok(new PagedResult<EmployeeDto>(items, page.Page, page.PageSize, page.TotalCount));
    }

    public async Task<Result<EmployeeSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var users = await Filter(new EmployeeQuery()).ToListAsync(cancellationToken);

        var dtos = await ToDtosAsync(users, cancellationToken);

        var withTarget = dtos.Where(dto => dto.IsActive && dto.PerformancePercent is not null).ToList();

        var tabs = new List<SummaryCount>
        {
            new("all", dtos.Count),

            new("active", dtos.Count(dto => dto.IsActive)),

            new("inactive", dtos.Count(dto => !dto.IsActive))
        };

        tabs.AddRange(MerchantRoles.Select(role => new SummaryCount(role, dtos.Count(dto => dto.Role == role))));

        return Result<EmployeeSummaryDto>.Ok(new EmployeeSummaryDto(dtos.Count, dtos.Count(dto => dto.IsActive), withTarget.Count == 0 ? null : Math.Round(withTarget.Average(dto => dto.PerformancePercent!.Value), 1), tabs));
    }

    public async Task<Result<EmployeeDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var employee = await LoadAsync(id, cancellationToken);

        return employee is null ? Result<EmployeeDto>.NotFound(NotFound) : Result<EmployeeDto>.Ok(employee);
    }

    public async Task<Result<EmployeeDto>> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var merchantId = userAppService.RequiredMerchantId;

        if (CheckRole(request.Role) is { } roleError)
        {
            return Result<EmployeeDto>.From(roleError);
        }

        var merchant = await context.Merchants.AsNoTracking().FirstAsync(item => item.Id == merchantId, cancellationToken);

        if (merchant.MaxUsers is { } max && await context.Users.CountAsync(user => user.MerchantId == merchantId, cancellationToken) >= max)
        {
            return Result<EmployeeDto>.Conflict($"Your plan allows {max} users. Contact support to add more.");
        }

        var contact = await CheckContactAsync(null, request.Phone, request.Email, cancellationToken);

        if (!contact.Succeeded)
        {
            return Result<EmployeeDto>.From(contact);
        }

        if (await CheckBranchAsync(request.Role, request.BranchId, cancellationToken) is { } branchError)
        {
            return Result<EmployeeDto>.From(branchError);
        }

        var (phone, email) = contact.Data;

        var user = new User
        {
            UserName = phone ?? email,

            PhoneNumber = phone,

            Email = email,

            EmailConfirmed = email is not null,

            FullName = request.FullName.Trim(),

            MerchantId = merchantId,

            BranchId = request.BranchId,

            JobTitle = Clean(request.JobTitle),

            MonthlyTarget = request.MonthlyTarget
        };

        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            return Result<EmployeeDto>.BadRequest(created.Errors.Select(error => error.Description));
        }

        await userManager.AddToRoleAsync(user, request.Role);

        return Result<EmployeeDto>.Created((await LoadAsync(user.Id, cancellationToken))!, "Employee created.");
    }

    public async Task<Result<EmployeeDto>> UpdateAsync(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);

        if (user is null)
        {
            return Result<EmployeeDto>.NotFound(NotFound);
        }

        var currentRole = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? RoleNames.Cashier;

        if (!CanManage(currentRole))
        {
            return Result<EmployeeDto>.Forbidden("Managers can only change cashiers.");
        }

        if (CheckRole(request.Role) is { } roleError)
        {
            return Result<EmployeeDto>.From(roleError);
        }

        var isSelf = user.Id == userAppService.RequiredUserId;

        if (isSelf && (request.Role != currentRole || !request.IsActive))
        {
            return Result<EmployeeDto>.Conflict("You cannot change your own role or turn off your own account.");
        }

        if (currentRole == RoleNames.MerchantOwner && (request.Role != RoleNames.MerchantOwner || !request.IsActive) && !await HasAnotherActiveOwnerAsync(user.Id, cancellationToken))
        {
            return Result<EmployeeDto>.Conflict("The store must keep at least one active owner.");
        }

        var contact = await CheckContactAsync(user.Id, request.Phone, request.Email, cancellationToken);

        if (!contact.Succeeded)
        {
            return Result<EmployeeDto>.From(contact);
        }

        if (await CheckBranchAsync(request.Role, request.BranchId, cancellationToken) is { } branchError)
        {
            return Result<EmployeeDto>.From(branchError);
        }

        var (phone, email) = contact.Data;

        user.FullName = request.FullName.Trim();

        user.PhoneNumber = phone;

        user.Email = email;

        user.UserName = phone ?? email;

        user.BranchId = request.BranchId;

        user.JobTitle = Clean(request.JobTitle);

        user.MonthlyTarget = request.MonthlyTarget;

        user.IsActive = request.IsActive;

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return Result<EmployeeDto>.BadRequest(updated.Errors.Select(error => error.Description));
        }

        if (request.Role != currentRole)
        {
            await userManager.RemoveFromRoleAsync(user, currentRole);

            await userManager.AddToRoleAsync(user, request.Role);
        }

        if (!request.IsActive || request.Role != currentRole)
        {
            // Signs the user out of other devices at their next request that checks the stamp, and blocks their next login.
            await userManager.UpdateSecurityStampAsync(user);
        }

        return Result<EmployeeDto>.Ok((await LoadAsync(id, cancellationToken))!, "Employee updated.");
    }

    public async Task<Result> ResetPasswordAsync(int id, ResetEmployeePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.NotFound(NotFound);
        }

        if (!CanManage((await userManager.GetRolesAsync(user)).FirstOrDefault() ?? RoleNames.Cashier))
        {
            return Result.Forbidden("Managers can only change cashiers.");
        }

        var reset = await PasswordReset.SetAsync(userManager, user, request.NewPassword);

        return reset.Succeeded ? Result.Ok("Password changed.") : Result.BadRequest(reset.Errors.Select(error => error.Description));
    }

    public async Task<Result<EmployeeDto>> SetAvatarAsync(int id, FileUpload? file, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);

        if (user is null)
        {
            return Result<EmployeeDto>.NotFound(NotFound);
        }

        if (file is null)
        {
            await images.DeleteAsync(user.AvatarPath, cancellationToken);

            user.AvatarPath = null;
        }

        else
        {
            var saved = await images.ReplaceAsync(user.AvatarPath, file, "users", "photo", cancellationToken);

            if (!saved.Succeeded)
            {
                return Result<EmployeeDto>.From(saved);
            }

            user.AvatarPath = saved.Data;
        }

        await userManager.UpdateAsync(user);

        return Result<EmployeeDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await FindAsync(id, cancellationToken);

        if (user is null)
        {
            return Result.NotFound(NotFound);
        }

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? RoleNames.Cashier;

        if (!CanManage(role))
        {
            return Result.Forbidden("Managers can only change cashiers.");
        }

        if (user.Id == userAppService.RequiredUserId)
        {
            return Result.Conflict("You cannot delete your own account.");
        }

        if (role == RoleNames.MerchantOwner && !await HasAnotherActiveOwnerAsync(user.Id, cancellationToken))
        {
            return Result.Conflict("The store must keep at least one active owner.");
        }

        if (await context.SalesInvoices.AnyAsync(invoice => invoice.CashierId == id, cancellationToken))
        {
            return Result.Conflict("This employee has invoices. Turn the account off instead of deleting it.");
        }

        user.IsDeleted = true;

        user.IsActive = false;

        await userManager.UpdateAsync(user);

        await userManager.UpdateSecurityStampAsync(user);

        return Result.Ok("Employee deleted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(EmployeeQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await ToDtosAsync(await Filter(query).OrderBy(row => row.User.FullName).ToListAsync(cancellationToken), cancellationToken);

        var csv = new CsvFile("Name", "Email", "Phone", "Role", "Job title", "Branch", "Monthly target (LYD)", "Sales this month (LYD)", "Performance %", "Active", "Last login");

        foreach (var row in rows)
        {
            csv.Row(row.FullName, row.Email, row.Phone, row.Role, row.JobTitle, row.BranchName, row.MonthlyTarget, row.SalesThisMonth, row.PerformancePercent, row.IsActive ? "Yes" : "No", row.LastLoginAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("employees"));
    }

    private IQueryable<Row> Filter(EmployeeQuery query)
    {
        var merchantId = userAppService.RequiredMerchantId;

        var users = context.Users.AsNoTracking().Where(user => user.MerchantId == merchantId);

        if (query.BranchId is { } branchId)
        {
            users = users.Where(user => user.BranchId == branchId);
        }

        if (query.IsActive is { } active)
        {
            users = users.Where(user => user.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            users = users.WhereMatchesSearch(query.Search);
        }

        var rows = users.Select(user => new Row
        {
            User = user,

            Role = context.UserRoles.Where(link => link.UserId == user.Id).Join(context.Roles, link => link.RoleId, role => role.Id, (link, role) => role.Name!).FirstOrDefault() ?? RoleNames.Cashier
        });

        return string.IsNullOrWhiteSpace(query.Role) ? rows : rows.Where(row => row.Role == query.Role);
    }

    private async Task<IReadOnlyList<EmployeeDto>> ToDtosAsync(IReadOnlyList<Row> rows, CancellationToken cancellationToken)
    {
        var ids = rows.Select(row => row.User.Id).ToList();

        var monthStart = LibyaTime.MonthStartUtc;

        var sales = await context.SalesInvoices.AsNoTracking()
            .Where(invoice => ids.Contains(invoice.CashierId) && invoice.Status == SalesInvoiceStatus.Completed && invoice.CompletedAt >= monthStart)
            .GroupBy(invoice => invoice.CashierId)
            .Select(group => new { CashierId = group.Key, Total = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount) })
            .ToDictionaryAsync(row => row.CashierId, row => row.Total, cancellationToken);

        var branchIds = rows.Where(row => row.User.BranchId != null).Select(row => row.User.BranchId!.Value).Distinct().ToList();

        var branches = await context.Branches.AsNoTracking().Where(branch => branchIds.Contains(branch.Id)).ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return rows
            .Select(row =>
            {
                var user = row.User;

                var month = Money.Round(sales.GetValueOrDefault(user.Id));

                return new EmployeeDto(
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.PhoneNumber,
                    FileUrls.UserAvatar(user.Id, user.AvatarPath, user.UpdatedAt ?? user.CreatedAt),
                    row.Role,
                    user.JobTitle,
                    user.BranchId,
                    user.BranchId is { } branchId ? branches.GetValueOrDefault(branchId) : null,
                    user.MonthlyTarget,
                    month,
                    user.MonthlyTarget is > 0 ? Math.Round(month * 100m / user.MonthlyTarget.Value, 1) : null,
                    user.IsActive,
                    user.LastLoginAt,
                    user.CreatedAt);
            })
            .ToList();
    }

    private async Task<EmployeeDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var row = await Filter(new EmployeeQuery()).FirstOrDefaultAsync(item => item.User.Id == id, cancellationToken);

        return row is null ? null : (await ToDtosAsync([row], cancellationToken))[0];
    }

    private Task<User?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var merchantId = userAppService.RequiredMerchantId;

        return userManager.Users.FirstOrDefaultAsync(user => user.Id == id && user.MerchantId == merchantId, cancellationToken);
    }

    /// <summary>Owners manage everyone; managers manage cashiers only.</summary>
    private bool CanManage(string role) => userAppService.IsOwner || role == RoleNames.Cashier;

    private Result? CheckRole(string role)
    {
        if (!MerchantRoles.Contains(role))
        {
            return Result.BadRequest($"Role must be one of: {string.Join(", ", MerchantRoles)}.");
        }

        return CanManage(role) ? null : Result.Forbidden("Only the owner can add or change owners and managers.");
    }

    private async Task<Result?> CheckBranchAsync(string role, int? branchId, CancellationToken cancellationToken)
    {
        if (branchId is null)
        {
            return role == RoleNames.Cashier ? Result.BadRequest("Choose the branch the cashier sells from.") : null;
        }

        var merchantId = userAppService.RequiredMerchantId;

        return await context.Branches.AnyAsync(branch => branch.Id == branchId && branch.MerchantId == merchantId, cancellationToken) ? null : Result.BadRequest("Branch was not found.");
    }

    private async Task<Result<(string? Phone, string? Email)>> CheckContactAsync(int? userId, string? phoneInput, string? emailInput, CancellationToken cancellationToken)
    {
        if (!LibyanPhone.TryNormalizeOptional(phoneInput, out var phone))
        {
            return Result<(string?, string?)>.BadRequest(LibyanPhone.InvalidMessage);
        }

        var email = string.IsNullOrWhiteSpace(emailInput) ? null : emailInput.Trim();

        if (phone is null && email is null)
        {
            return Result<(string?, string?)>.BadRequest("Enter a mobile number or an email to sign in with.");
        }

        if (phone is not null && await userManager.Users.AnyAsync(user => user.Id != userId && (user.PhoneNumber == phone || user.UserName == phone), cancellationToken))
        {
            return Result<(string?, string?)>.Conflict("This mobile number is already registered.");
        }

        if (email is not null && await userManager.FindByEmailAsync(email) is { } other && other.Id != userId)
        {
            return Result<(string?, string?)>.Conflict("This email is already registered.");
        }

        return Result<(string?, string?)>.Ok((phone, email));
    }

    private async Task<bool> HasAnotherActiveOwnerAsync(int userId, CancellationToken cancellationToken)
    {
        var merchantId = userAppService.RequiredMerchantId;

        var owners = await userManager.GetUsersInRoleAsync(RoleNames.MerchantOwner);

        return owners.Any(owner => owner.Id != userId && owner.MerchantId == merchantId && owner.IsActive && !owner.IsDeleted);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Staff;

using MicroNamaa.Application.Services.Staff;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

using MicroNamaa.Infrastructure.Services.Auth;

namespace MicroNamaa.Infrastructure.Services.Staff;

/// <summary>Sub-admins: platform admins limited to the areas and actions the owner grants them.</summary>
public class StaffService(UserManager<User> userManager, IUnitOfWork unitOfWork) : IStaffService
{
    private const string NotFound = "Staff member was not found.";

    public async Task<Result<IReadOnlyList<StaffMemberDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.AsNoTracking()
            .Where(user => user.IsStaff)
            .Include(user => user.StaffPermissions)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<StaffMemberDto>>.Ok(users.Select(ToDto).ToList());
    }

    public async Task<Result<StaffMemberDto>> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();

        string? phone = null;

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = LibyanPhone.Normalize(request.Phone);

            if (phone is null)
            {
                return Result<StaffMemberDto>.BadRequest("Enter a valid Libyan mobile number (09xxxxxxxx).");
            }

            if (await userManager.Users.AnyAsync(user => user.PhoneNumber == phone, cancellationToken))
            {
                return Result<StaffMemberDto>.Conflict("This mobile number is already registered.");
            }
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result<StaffMemberDto>.Conflict("This email is already registered.");
        }

        var user = new User
        {
            UserName = email,

            Email = email,

            EmailConfirmed = true,

            PhoneNumber = phone,

            FullName = request.FullName.Trim(),

            IsStaff = true
        };

        var created = await userManager.CreateAsync(user, request.Password);

        if (!created.Succeeded)
        {
            return Result<StaffMemberDto>.BadRequest(created.Errors.Select(error => error.Description));
        }

        await userManager.AddToRoleAsync(user, RoleNames.PlatformAdmin);

        await ApplyPermissionsAsync(user.Id, request.Permissions, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StaffMemberDto>.Created((await LoadAsync(user.Id, cancellationToken))!, "Staff member created.");
    }

    public async Task<Result<StaffMemberDto>> UpdateAsync(int id, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Id == id && item.IsStaff, cancellationToken);

        if (user is null)
        {
            return Result<StaffMemberDto>.NotFound(NotFound);
        }

        user.FullName = request.FullName.Trim();

        user.IsActive = request.IsActive;

        await userManager.UpdateAsync(user);

        await ApplyPermissionsAsync(user.Id, request.Permissions, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<StaffMemberDto>.Ok((await LoadAsync(user.Id, cancellationToken))!, "Staff member updated.");
    }

    public async Task<Result> ResetPasswordAsync(int id, ResetStaffPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(item => item.Id == id && item.IsStaff, cancellationToken);

        if (user is null)
        {
            return Result.NotFound(NotFound);
        }

        var reset = await PasswordReset.SetAsync(userManager, user, request.NewPassword);

        return reset.Succeeded ? Result.Ok("Password changed.") : Result.BadRequest(reset.Errors.Select(error => error.Description));
    }

    /// <summary>Keeps one row per area and only changes its actions, so the audit log shows exactly what was granted or taken away.</summary>
    private async Task ApplyPermissionsAsync(int userId, IReadOnlyList<StaffPermissionDto> requested, CancellationToken cancellationToken)
    {
        var wanted = requested
            .Where(item => Enum.IsDefined(item.Area))
            .GroupBy(item => item.Area)
            .ToDictionary(group => group.Key, group => Normalize(group.Aggregate(AdminActions.None, (all, item) => all | item.Actions)));

        var repository = unitOfWork.Repository<StaffPermission>();

        var existing = await repository.Query(asNoTracking: false).Where(item => item.UserId == userId).ToListAsync(cancellationToken);

        foreach (var area in Enum.GetValues<AdminArea>())
        {
            var actions = wanted.GetValueOrDefault(area, AdminActions.None);

            var row = existing.FirstOrDefault(item => item.Area == area);

            if (row is null)
            {
                if (actions != AdminActions.None)
                {
                    await repository.AddAsync(new StaffPermission { UserId = userId, Area = area, Actions = actions }, cancellationToken);
                }
            }

            else if (row.Actions != actions)
            {
                row.Actions = actions;
            }
        }
    }

    /// <summary>Any action implies seeing the screen; unknown bits are dropped.</summary>
    private static AdminActions Normalize(AdminActions actions)
    {
        var known = actions & (AdminActions.View | AdminActions.Create | AdminActions.Update | AdminActions.Delete);

        return known == AdminActions.None ? AdminActions.None : known | AdminActions.View;
    }

    private async Task<StaffMemberDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var user = await userManager.Users.AsNoTracking().Include(item => item.StaffPermissions).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return user is null ? null : ToDto(user);
    }

    private static StaffMemberDto ToDto(User user) => new(
        user.Id,
        user.FullName,
        user.Email,
        user.PhoneNumber,
        user.IsActive,
        user.LastLoginAt,
        user.CreatedAt,
        user.StaffPermissions.Where(item => item.Actions != AdminActions.None).OrderBy(item => item.Area).Select(item => new StaffPermissionDto(item.Area, item.Actions)).ToList());
}

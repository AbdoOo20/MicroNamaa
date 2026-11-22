using System.Security.Claims;

using System.Text;

using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using Microsoft.IdentityModel.JsonWebTokens;

using Microsoft.IdentityModel.Tokens;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Auth;

using MicroNamaa.Application.DTOs.Staff;

using MicroNamaa.Application.Services.Auth;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Infrastructure.Services.Auth;

public class AuthService(UserManager<User> userManager, IUnitOfWork unitOfWork, IUserAppService userAppService, IMerchantAccessService merchantAccess, ImageUploads images, IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings jwt = jwtOptions.Value;

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        const string invalidCredentials = "Invalid login or password.";

        var user = await FindByLoginAsync(request.Login, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<AuthResponse>.Unauthorized(invalidCredentials);
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return Result<AuthResponse>.Forbidden("Account is locked. Try again later.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);

            return Result<AuthResponse>.Unauthorized(invalidCredentials);
        }

        if (user.MerchantId is { } merchantId && !await merchantAccess.IsAllowedAsync(merchantId, cancellationToken))
        {
            return Result<AuthResponse>.Forbidden(IMerchantAccessService.BlockedMessage);
        }

        await userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTime.UtcNow;

        await userManager.UpdateAsync(user);

        return Result<AuthResponse>.Ok(await BuildResponseAsync(user));
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var dto = await LoadCurrentAsync(cancellationToken);

        return dto is null ? Result<CurrentUserDto>.NotFound("User was not found.") : Result<CurrentUserDto>.Ok(dto);
    }

    public async Task<Result<CurrentUserDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userAppService.RequiredUserId.ToString());

        if (user is null)
        {
            return Result<CurrentUserDto>.NotFound("User was not found.");
        }

        var phone = user.PhoneNumber;

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            phone = LibyanPhone.Normalize(request.Phone);

            if (phone is null)
            {
                return Result<CurrentUserDto>.BadRequest(LibyanPhone.InvalidMessage);
            }

            if (phone != user.PhoneNumber && await userManager.Users.AnyAsync(item => item.Id != user.Id && item.PhoneNumber == phone, cancellationToken))
            {
                return Result<CurrentUserDto>.Conflict("This mobile number is already registered.");
            }
        }

        var email = string.IsNullOrWhiteSpace(request.Email) ? user.Email : request.Email.Trim();

        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase) && email is not null && await userManager.FindByEmailAsync(email) is { } other && other.Id != user.Id)
        {
            return Result<CurrentUserDto>.Conflict("This email is already registered.");
        }

        user.FullName = request.FullName.Trim();

        user.PhoneNumber = phone;

        user.Email = email;

        // The login name follows the mobile number, or the email when there is no number.
        user.UserName = phone ?? email ?? user.UserName;

        var updated = await userManager.UpdateAsync(user);

        if (!updated.Succeeded)
        {
            return Result<CurrentUserDto>.BadRequest(updated.Errors.Select(error => error.Description));
        }

        return Result<CurrentUserDto>.Ok((await LoadCurrentAsync(cancellationToken))!, "Profile updated.");
    }

    public async Task<Result<CurrentUserDto>> SetAvatarAsync(FileUpload? file, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userAppService.RequiredUserId.ToString());

        if (user is null)
        {
            return Result<CurrentUserDto>.NotFound("User was not found.");
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
                return Result<CurrentUserDto>.From(saved);
            }

            user.AvatarPath = saved.Data;
        }

        await userManager.UpdateAsync(user);

        return Result<CurrentUserDto>.Ok((await LoadCurrentAsync(cancellationToken))!);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userAppService.RequiredUserId.ToString());

        if (user is null)
        {
            return Result.NotFound("User was not found.");
        }

        var changed = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        return changed.Succeeded ? Result.Ok("Password changed.") : Result.BadRequest(changed.Errors.Select(error => error.Description));
    }

    private async Task<CurrentUserDto?> LoadCurrentAsync(CancellationToken cancellationToken)
    {
        var userId = userAppService.RequiredUserId;

        var user = await userManager.Users.AsNoTracking()
            .Include(item => item.Merchant)
            .Include(item => item.Branch)
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await userManager.GetRolesAsync(user);

        IReadOnlyList<StaffPermissionDto>? permissions = null;

        if (user.IsStaff)
        {
            permissions = await unitOfWork.Repository<StaffPermission>().Query()
                .Where(item => item.UserId == user.Id && item.Actions != AdminActions.None)
                .OrderBy(item => item.Area)
                .Select(item => new StaffPermissionDto(item.Area, item.Actions))
                .ToListAsync(cancellationToken);
        }

        var merchant = user.Merchant;

        return new CurrentUserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            FileUrls.UserAvatar(user.Id, user.AvatarPath, user.UpdatedAt ?? user.CreatedAt),
            roles.FirstOrDefault() ?? string.Empty,
            user.JobTitle,
            user.MerchantId,
            merchant?.Code,
            merchant?.Name,
            merchant is null ? null : FileUrls.MerchantLogo(merchant.Id, merchant.LogoPath, merchant.UpdatedAt),
            merchant?.SubscriptionEndsAt,
            user.BranchId,
            user.Branch?.Name,
            user.IsStaff,
            permissions);
    }

    private async Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        var value = login.Trim();

        if (value.Contains('@'))
        {
            return await userManager.FindByEmailAsync(value);
        }

        var phone = LibyanPhone.Normalize(value);

        return phone is null ? await userManager.FindByNameAsync(value) : await userManager.Users.FirstOrDefaultAsync(user => user.PhoneNumber == phone, cancellationToken);
    }

    private async Task<AuthResponse> BuildResponseAsync(User user)
    {
        var roles = await userManager.GetRolesAsync(user);

        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(AppClaimTypes.UserId, user.Id.ToString()),

            new("name", user.FullName)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim("email", user.Email));
        }

        if (user.MerchantId is { } merchantId)
        {
            claims.Add(new Claim(AppClaimTypes.MerchantId, merchantId.ToString()));
        }

        if (user.BranchId is { } branchId)
        {
            claims.Add(new Claim(AppClaimTypes.BranchId, branchId.ToString()));
        }

        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),

            Issuer = jwt.Issuer,

            Audience = jwt.Audience,

            Expires = expiresAt,

            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new AuthResponse(token, expiresAt, user.Id, user.MerchantId, user.BranchId, user.FullName, roles.FirstOrDefault() ?? string.Empty);
    }
}

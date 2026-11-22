using MicroNamaa.Application.DTOs.Staff;

namespace MicroNamaa.Application.DTOs.Auth;

/// <summary>
/// The signed-in user. Merchant users get their merchant, branch and subscription; platform admins get IsStaff and Permissions
/// (the owner admin has IsStaff false and may do everything).
/// </summary>
public record CurrentUserDto(
    int UserId,
    string FullName,
    string? Email,
    string? Phone,
    string? AvatarUrl,
    string Role,
    string? JobTitle,
    int? MerchantId,
    string? MerchantCode,
    string? MerchantName,
    string? MerchantLogoUrl,
    DateTime? SubscriptionEndsAt,
    int? BranchId,
    string? BranchName,
    bool IsStaff = false,
    IReadOnlyList<StaffPermissionDto>? Permissions = null);

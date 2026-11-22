using MicroNamaa.Domain.Constants;

namespace MicroNamaa.Api.Authorization;

/// <summary>Role lists for <c>[Authorize(Roles = ...)]</c> on controllers.</summary>
public static class AppRoles
{
    public const string Admin = RoleNames.PlatformAdmin;

    public const string Owner = RoleNames.MerchantOwner;

    public const string Managers = RoleGroups.Managers;

    public const string MerchantUsers = RoleGroups.MerchantUsers;

    /// <summary>The help center: store owners and managers open tickets, platform admins answer them.</summary>
    public const string Support = RoleNames.PlatformAdmin + "," + RoleGroups.Managers;
}

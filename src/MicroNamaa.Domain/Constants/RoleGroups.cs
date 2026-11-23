namespace MicroNamaa.Domain.Constants;

/// <summary>Comma-separated role lists for <c>[Authorize(Roles = ...)]</c>.</summary>
public static class RoleGroups
{
    public const string Managers = RoleNames.MerchantOwner + "," + RoleNames.MerchantManager;

    public const string MerchantUsers = RoleNames.MerchantOwner + "," + RoleNames.MerchantManager + "," + RoleNames.Cashier;
}

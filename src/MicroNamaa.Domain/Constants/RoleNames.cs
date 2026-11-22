namespace MicroNamaa.Domain.Constants;

public static class RoleNames
{
    /// <summary>The platform owner and its staff (sub-admins). Not tied to a merchant.</summary>
    public const string PlatformAdmin = "PlatformAdmin";

    /// <summary>The merchant who bought the system. One or more per merchant; manages everything including settings and managers.</summary>
    public const string MerchantOwner = "MerchantOwner";

    /// <summary>A merchant employee who runs the store: products, stock, purchases, customers, reports.</summary>
    public const string MerchantManager = "MerchantManager";

    /// <summary>A merchant employee who sells from the POS of their branch.</summary>
    public const string Cashier = "Cashier";
}

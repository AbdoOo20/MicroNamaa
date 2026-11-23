namespace MicroNamaa.Application.Common;

/// <summary>
/// The address the frontend puts in an &lt;img&gt;. Images are served by <c>FilesController</c>, never as static files;
/// <c>v</c> changes when the image changes so browsers do not keep an old one.
/// </summary>
public static class FileUrls
{
    public static string? Product(int id, string? path, DateTime? changedAt) => Build("products", id, "image", path, changedAt);

    public static string? Category(int id, string? path, DateTime? changedAt) => Build("categories", id, "image", path, changedAt);

    public static string? UserAvatar(int id, string? path, DateTime? changedAt) => Build("users", id, "avatar", path, changedAt);

    public static string? CustomerAvatar(int id, string? path, DateTime? changedAt) => Build("customers", id, "avatar", path, changedAt);

    public static string? SupplierLogo(int id, string? path, DateTime? changedAt) => Build("suppliers", id, "logo", path, changedAt);

    public static string? MerchantLogo(int id, string? path, DateTime? changedAt) => Build("merchants", id, "logo", path, changedAt);

    private static string? Build(string area, int id, string kind, string? path, DateTime? changedAt) =>
        string.IsNullOrEmpty(path) ? null : $"/api/files/{area}/{id}/{kind}?v={(changedAt ?? DateTime.MinValue).Ticks:x}";
}

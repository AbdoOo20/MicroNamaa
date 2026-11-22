using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Services.Files;

/// <summary>Streams the stored images used in &lt;img&gt; tags (product photos, logos, avatars).</summary>
public interface IImageService
{
    /// <param name="area">products, categories, users, customers, suppliers or merchants.</param>
    /// <param name="id">The id of the product, category, user, customer, supplier or merchant.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<Result<FileDownload>> GetAsync(string area, int id, CancellationToken cancellationToken = default);
}

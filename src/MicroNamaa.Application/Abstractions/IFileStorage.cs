using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Abstractions;

/// <summary>Stores uploaded images (products, logos, avatars). Files are kept outside wwwroot and only served through the API.</summary>
public interface IFileStorage
{
    /// <summary>Saves the file under <paramref name="folder" /> and returns its relative path.</summary>
    Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default);

    Task<FileDownload?> OpenAsync(string relativePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

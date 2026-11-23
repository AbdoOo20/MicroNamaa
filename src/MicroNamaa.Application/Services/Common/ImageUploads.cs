using Microsoft.Extensions.Options;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Settings;

namespace MicroNamaa.Application.Services.Common;

/// <summary>Checks and stores an uploaded image and removes the one it replaces.</summary>
public class ImageUploads(IFileStorage storage, IOptions<PlatformSettings> platformOptions)
{
    /// <returns>The new relative path, or the error.</returns>
    public async Task<Result<string>> ReplaceAsync(string? oldPath, FileUpload? file, string folder, string label, CancellationToken cancellationToken = default)
    {
        if (UploadRules.ValidateImage(file, platformOptions.Value.MaxUploadSizeMb, label) is { } error)
        {
            return Result<string>.BadRequest(error);
        }

        var path = await storage.SaveAsync(file!, folder, cancellationToken);

        await DeleteAsync(oldPath, cancellationToken);

        return Result<string>.Ok(path);
    }

    public async Task DeleteAsync(string? path, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(path))
        {
            await storage.DeleteAsync(path, cancellationToken);
        }
    }
}

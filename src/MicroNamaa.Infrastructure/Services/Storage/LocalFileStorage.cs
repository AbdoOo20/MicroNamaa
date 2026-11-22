using Microsoft.Extensions.Options;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Exceptions;

using MicroNamaa.Application.Settings;

namespace MicroNamaa.Infrastructure.Services.Storage;

/// <summary>
/// Keeps uploads on the server's disk, outside wwwroot, so they are only reachable through the API's permission checks.
/// The content is checked against the file's real type, not only its extension.
/// </summary>
public class LocalFileStorage(IOptions<StorageSettings> storageOptions) : IFileStorage
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",

        [".jpeg"] = "image/jpeg",

        [".png"] = "image/png",

        [".webp"] = "image/webp",

        [".pdf"] = "application/pdf"
    };

    private string Root => Path.GetFullPath(storageOptions.Value.RootPath);

    public async Task<string> SaveAsync(FileUpload file, string folder, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".jpeg")
        {
            extension = ".jpg";
        }

        var header = new byte[12];

        await using var buffered = new MemoryStream();

        await file.Content.CopyToAsync(buffered, cancellationToken);

        buffered.Position = 0;

        var read = await buffered.ReadAsync(header, cancellationToken);

        if (!MatchesSignature(extension, header.AsSpan(0, read)))
        {
            throw new BadRequestException("The file content does not match its type.");
        }

        var now = DateTime.UtcNow;

        var relative = Path.Combine(folder, now.ToString("yyyy"), now.ToString("MM"), $"{Guid.NewGuid():N}{extension}");

        var full = Path.Combine(Root, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        buffered.Position = 0;

        await using (var target = File.Create(full))
        {
            await buffered.CopyToAsync(target, cancellationToken);
        }

        return relative.Replace('\\', '/');
    }

    public Task<FileDownload?> OpenAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var full = Resolve(relativePath);

        if (full is null || !File.Exists(full))
        {
            return Task.FromResult<FileDownload?>(null);
        }

        var extension = Path.GetExtension(full);

        Stream stream = File.OpenRead(full);

        return Task.FromResult<FileDownload?>(new FileDownload(stream, ContentTypes.GetValueOrDefault(extension, "application/octet-stream"), Path.GetFileName(full)));
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var full = Resolve(relativePath);

        if (full is not null && File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    private string? Resolve(string relativePath)
    {
        var root = Root;

        var full = Path.GetFullPath(Path.Combine(root, relativePath));

        return full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) ? full : null;
    }

    private static bool MatchesSignature(string extension, ReadOnlySpan<byte> header) => extension switch
    {
        ".jpg" => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),

        ".png" => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }),

        ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),

        ".pdf" => header.StartsWith("%PDF"u8),

        _ => false
    };
}

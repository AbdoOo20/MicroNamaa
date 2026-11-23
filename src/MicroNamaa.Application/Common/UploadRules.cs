namespace MicroNamaa.Application.Common;

public static class UploadRules
{
    public static readonly IReadOnlySet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    /// <summary>Returns an error message when the image is missing, empty, too large or not a JPG, PNG or WEBP file.</summary>
    public static string? ValidateImage(FileUpload? file, int maxSizeMb, string label)
    {
        if (file is null || file.Length == 0)
        {
            return $"The {label} is required.";
        }

        if (file.Length > maxSizeMb * 1024L * 1024L)
        {
            return $"The {label} must be {maxSizeMb} MB or smaller.";
        }

        var extension = Path.GetExtension(file.FileName);

        return ImageExtensions.Contains(extension) ? null : $"The {label} must be a JPG, PNG or WEBP image.";
    }
}

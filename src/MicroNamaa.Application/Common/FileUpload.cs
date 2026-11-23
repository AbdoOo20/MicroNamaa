namespace MicroNamaa.Application.Common;

/// <summary>A file sent by a client, independent of ASP.NET's IFormFile.</summary>
public record FileUpload(Stream Content, string FileName, string? ContentType, long Length);

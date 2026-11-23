using MicroNamaa.Application.Common;

namespace MicroNamaa.Api.Extensions;

public static class FormFileExtensions
{
    public static FileUpload? ToUpload(this IFormFile? file) => file is null ? null : new FileUpload(file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
}

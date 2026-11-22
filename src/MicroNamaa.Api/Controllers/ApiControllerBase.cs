using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Extensions;

using MicroNamaa.Application.Common;

namespace MicroNamaa.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult Respond(Result result) => StatusCode(result.StatusCode, result);

    /// <summary>Streams the file when the result succeeded, otherwise returns the usual JSON error.</summary>
    protected IActionResult Download(Result<FileDownload> result) =>
        result is { Succeeded: true, Data: { } file } ? File(file.Content, file.ContentType, file.FileName) : Respond(result);

    /// <summary>The uploaded file of a multipart request, or null.</summary>
    protected static FileUpload? Upload(IFormFile? file) => file.ToUpload();
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Services.Files;

namespace MicroNamaa.Api.Controllers;

/// <summary>
/// Serves the stored images for &lt;img&gt; tags: /api/files/{products|categories|users|customers|suppliers|merchants}/{id}/{image|avatar|logo}.
/// The DTOs return these addresses ready to use (imageUrl, avatarUrl, logoUrl).
/// </summary>
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicies.Public)]
public class FilesController(IImageService imageService) : ApiControllerBase
{
    [HttpGet("{area}/{id:int}/{kind}")]
    [ResponseCache(Duration = 604800, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Get(string area, int id, CancellationToken cancellationToken)
    {
        return Download(await imageService.GetAsync(area, id, cancellationToken));
    }
}

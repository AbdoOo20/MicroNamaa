using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Application.Services.Catalog;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.MerchantUsers)]
public class CategoriesController(ICategoryService categoryService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<CategoryDto>>>> GetAll([FromQuery] CategoryQuery query, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<CategorySummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await categoryService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Export([FromQuery] CategoryQuery query, CancellationToken cancellationToken)
    {
        return Download(await categoryService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<CategoryDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<CategoryDto>>> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<CategoryDto>>> Update(int id, CategoryRequest request, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.DeleteAsync(id, cancellationToken));
    }

    /// <summary>multipart/form-data with the image in "file".</summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = AppRoles.Managers)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<CategoryDto>>> SetImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.SetImageAsync(id, Upload(file), cancellationToken));
    }

    [HttpDelete("{id:int}/image")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<CategoryDto>>> RemoveImage(int id, CancellationToken cancellationToken)
    {
        return Respond(await categoryService.SetImageAsync(id, null, cancellationToken));
    }
}

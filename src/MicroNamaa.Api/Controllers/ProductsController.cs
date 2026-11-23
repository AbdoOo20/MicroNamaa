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
public class ProductsController(IProductService productService) : ApiControllerBase
{
    /// <summary>The products table: ?status=&amp;stockState=&amp;categoryId=&amp;warehouseId=&amp;sort=name|price|quantity|newest&amp;search=</summary>
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<ProductListItemDto>>>> GetAll([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        return Respond(await productService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<ProductSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await productService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Export([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        return Download(await productService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<ProductDetailDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await productService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<ProductDetailDto>>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        return Respond(await productService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<ProductDetailDto>>> Update(int id, ProductRequest request, CancellationToken cancellationToken)
    {
        return Respond(await productService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await productService.DeleteAsync(id, cancellationToken));
    }

    /// <summary>multipart/form-data with the photo in "file" (JPG, PNG or WEBP, 5 MB).</summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Roles = AppRoles.Managers)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<ProductDetailDto>>> SetImage(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await productService.SetImageAsync(id, Upload(file), cancellationToken));
    }

    [HttpDelete("{id:int}/image")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<ProductDetailDto>>> RemoveImage(int id, CancellationToken cancellationToken)
    {
        return Respond(await productService.SetImageAsync(id, null, cancellationToken));
    }
}

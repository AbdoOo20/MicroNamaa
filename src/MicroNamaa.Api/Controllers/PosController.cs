using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Sales;

namespace MicroNamaa.Api.Controllers;

/// <summary>The cashier screen (Home).</summary>
[Authorize(Roles = AppRoles.MerchantUsers)]
public class PosController(IPosService posService) : ApiControllerBase
{
    [HttpGet("context")]
    public async Task<ActionResult<Result<PosContextDto>>> GetContext([FromQuery] int? branchId, CancellationToken cancellationToken)
    {
        return Respond(await posService.GetContextAsync(branchId, cancellationToken));
    }

    /// <summary>Product cards with the branch's stock: ?branchId=&amp;categoryId=&amp;inStockOnly=&amp;search=</summary>
    [HttpGet("products")]
    public async Task<ActionResult<Result<PagedResult<PosProductDto>>>> GetProducts([FromQuery] PosProductQuery query, CancellationToken cancellationToken)
    {
        return Respond(await posService.GetProductsAsync(query, cancellationToken));
    }

    /// <summary>Barcode scan or exact SKU: ?code=&amp;branchId=</summary>
    [HttpGet("products/lookup")]
    public async Task<ActionResult<Result<PosProductDto>>> Lookup([FromQuery] string code, [FromQuery] int? branchId, CancellationToken cancellationToken)
    {
        return Respond(await posService.LookupAsync(code, branchId, cancellationToken));
    }
}

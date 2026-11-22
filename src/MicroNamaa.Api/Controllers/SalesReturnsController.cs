using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Sales;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.MerchantUsers)]
[Route("api/sales/returns")]
public class SalesReturnsController(ISalesReturnService returnService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<SalesReturnListItemDto>>>> GetAll([FromQuery] SalesReturnQuery query, CancellationToken cancellationToken)
    {
        return Respond(await returnService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Export([FromQuery] SalesReturnQuery query, CancellationToken cancellationToken)
    {
        return Download(await returnService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<SalesReturnDto>>> Get(long id, CancellationToken cancellationToken)
    {
        return Respond(await returnService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<SalesReturnDto>>> Create(CreateSalesReturnRequest request, CancellationToken cancellationToken)
    {
        return Respond(await returnService.CreateAsync(request, cancellationToken));
    }
}

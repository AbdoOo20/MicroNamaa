using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

using MicroNamaa.Application.Services.Inventory;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class WarehousesController(IWarehouseService warehouseService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<WarehouseDto>>>> GetAll([FromQuery] WarehouseQuery query, CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Result<WarehouseSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] WarehouseQuery query, CancellationToken cancellationToken)
    {
        return Download(await warehouseService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<WarehouseDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<WarehouseDto>>> Create(WarehouseRequest request, CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<WarehouseDto>>> Update(int id, WarehouseRequest request, CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await warehouseService.DeleteAsync(id, cancellationToken));
    }
}

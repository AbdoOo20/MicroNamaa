using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Inventory;

using MicroNamaa.Application.Services.Inventory;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class InventoryController(IInventoryService inventoryService) : ApiControllerBase
{
    /// <summary>Stock per product and warehouse: ?warehouseId=&amp;productId=&amp;stockState=&amp;search=</summary>
    [HttpGet("stock")]
    public async Task<ActionResult<Result<PagedResult<StockRowDto>>>> GetStock([FromQuery] StockQuery query, CancellationToken cancellationToken)
    {
        return Respond(await inventoryService.GetStockAsync(query, cancellationToken));
    }

    [HttpGet("movements")]
    public async Task<ActionResult<Result<PagedResult<StockMovementDto>>>> GetMovements([FromQuery] StockMovementQuery query, CancellationToken cancellationToken)
    {
        return Respond(await inventoryService.GetMovementsAsync(query, cancellationToken));
    }

    [HttpPost("adjustments")]
    public async Task<ActionResult<Result<StockOperationResultDto>>> Adjust(StockAdjustmentRequest request, CancellationToken cancellationToken)
    {
        return Respond(await inventoryService.AdjustAsync(request, cancellationToken));
    }

    [HttpPost("transfers")]
    public async Task<ActionResult<Result<StockOperationResultDto>>> Transfer(StockTransferRequest request, CancellationToken cancellationToken)
    {
        return Respond(await inventoryService.TransferAsync(request, cancellationToken));
    }
}

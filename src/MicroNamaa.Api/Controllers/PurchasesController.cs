using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Purchases;

using MicroNamaa.Application.Services.Purchases;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class PurchasesController(IPurchaseOrderService purchaseService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<PurchaseOrderListItemDto>>>> GetAll([FromQuery] PurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Result<PurchaseSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] PurchaseOrderQuery query, CancellationToken cancellationToken)
    {
        return Download(await purchaseService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> Get(long id, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> Create(SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> Update(long id, SavePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/status")]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> UpdateStatus(long id, UpdatePurchaseStatusRequest request, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.UpdateStatusAsync(id, request, cancellationToken));
    }

    /// <summary>The goods arrived: stock, average cost and the supplier's balance change now.</summary>
    [HttpPost("{id:long}/deliver")]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> Deliver(long id, DeliverPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.DeliverAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<Result<PurchaseOrderDto>>> Cancel(long id, ReasonRequest request, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.CancelAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<Result>> DeleteDraft(long id, CancellationToken cancellationToken)
    {
        return Respond(await purchaseService.DeleteDraftAsync(id, cancellationToken));
    }
}

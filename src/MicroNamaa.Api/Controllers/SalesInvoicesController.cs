using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Common;

using MicroNamaa.Application.DTOs.Sales;

using MicroNamaa.Application.Services.Sales;

namespace MicroNamaa.Api.Controllers;

/// <summary>POS invoices. Cashiers see and change their own; owners and managers see all and can cancel.</summary>
[Authorize(Roles = AppRoles.MerchantUsers)]
[Route("api/sales/invoices")]
public class SalesInvoicesController(ISalesInvoiceService invoiceService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<SalesInvoiceListItemDto>>>> GetAll([FromQuery] SalesInvoiceQuery query, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Result<SalesInvoiceSummaryDto>>> GetSummary([FromQuery] SalesInvoiceQuery query, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.GetSummaryAsync(query, cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Export([FromQuery] SalesInvoiceQuery query, CancellationToken cancellationToken)
    {
        return Download(await invoiceService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<SalesInvoiceDto>>> Get(long id, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.GetAsync(id, cancellationToken));
    }

    /// <summary>Prices the cart (promotions, points, tax) without saving.</summary>
    [HttpPost("preview")]
    public async Task<ActionResult<Result<InvoicePreviewDto>>> Preview(SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.PreviewAsync(request, cancellationToken));
    }

    /// <summary>complete=false saves a draft (Save As Draft / Hold); complete=true with payment finishes the sale.</summary>
    [HttpPost]
    public async Task<ActionResult<Result<SalesInvoiceDto>>> Create(SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<Result<SalesInvoiceDto>>> Update(long id, SaveSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/complete")]
    public async Task<ActionResult<Result<SalesInvoiceDto>>> Complete(long id, CompleteSalesInvoiceRequest request, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.CompleteAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<SalesInvoiceDto>>> Cancel(long id, ReasonRequest request, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.CancelAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult<Result>> DeleteDraft(long id, CancellationToken cancellationToken)
    {
        return Respond(await invoiceService.DeleteDraftAsync(id, cancellationToken));
    }
}

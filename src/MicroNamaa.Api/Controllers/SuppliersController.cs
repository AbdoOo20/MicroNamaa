using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

using MicroNamaa.Application.Services.Parties;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class SuppliersController(ISupplierService supplierService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<SupplierListItemDto>>>> GetAll([FromQuery] SupplierQuery query, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Result<SupplierSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await supplierService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] SupplierQuery query, CancellationToken cancellationToken)
    {
        return Download(await supplierService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<SupplierDetailDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<SupplierDetailDto>>> Create(SupplierRequest request, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<SupplierDetailDto>>> Update(int id, SupplierRequest request, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.DeleteAsync(id, cancellationToken));
    }

    /// <summary>multipart/form-data with the logo in "file".</summary>
    [HttpPost("{id:int}/logo")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<SupplierDetailDto>>> SetLogo(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.SetLogoAsync(id, Upload(file), cancellationToken));
    }

    [HttpDelete("{id:int}/logo")]
    public async Task<ActionResult<Result<SupplierDetailDto>>> RemoveLogo(int id, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.SetLogoAsync(id, null, cancellationToken));
    }

    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<Result<PagedResult<BalanceEntryDto>>>> GetStatement(int id, [FromQuery] BalanceEntryQuery query, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.GetStatementAsync(id, query, cancellationToken));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<Result<BalanceEntryDto>>> Pay(int id, PaymentRequest request, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.PayAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/adjustments")]
    public async Task<ActionResult<Result<BalanceEntryDto>>> Adjust(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken)
    {
        return Respond(await supplierService.AdjustBalanceAsync(id, request, cancellationToken));
    }
}

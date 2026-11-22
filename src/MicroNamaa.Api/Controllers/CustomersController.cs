using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

using MicroNamaa.Application.Services.Parties;

namespace MicroNamaa.Api.Controllers;

/// <summary>Cashiers can find, add and edit customers and take their payments; the rest is for owners and managers.</summary>
[Authorize(Roles = AppRoles.MerchantUsers)]
public class CustomersController(ICustomerService customerService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<CustomerListItemDto>>>> GetAll([FromQuery] CustomerQuery query, CancellationToken cancellationToken)
    {
        return Respond(await customerService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<CustomerSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await customerService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<IActionResult> Export([FromQuery] CustomerQuery query, CancellationToken cancellationToken)
    {
        return Download(await customerService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<CustomerDetailDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await customerService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<CustomerDetailDto>>> Create(CustomerRequest request, CancellationToken cancellationToken)
    {
        return Respond(await customerService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<CustomerDetailDto>>> Update(int id, CustomerRequest request, CancellationToken cancellationToken)
    {
        return Respond(await customerService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await customerService.DeleteAsync(id, cancellationToken));
    }

    /// <summary>multipart/form-data with the photo in "file".</summary>
    [HttpPost("{id:int}/avatar")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<CustomerDetailDto>>> SetAvatar(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await customerService.SetAvatarAsync(id, Upload(file), cancellationToken));
    }

    [HttpDelete("{id:int}/avatar")]
    public async Task<ActionResult<Result<CustomerDetailDto>>> RemoveAvatar(int id, CancellationToken cancellationToken)
    {
        return Respond(await customerService.SetAvatarAsync(id, null, cancellationToken));
    }

    [HttpGet("{id:int}/statement")]
    public async Task<ActionResult<Result<PagedResult<BalanceEntryDto>>>> GetStatement(int id, [FromQuery] BalanceEntryQuery query, CancellationToken cancellationToken)
    {
        return Respond(await customerService.GetStatementAsync(id, query, cancellationToken));
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<Result<BalanceEntryDto>>> ReceivePayment(int id, PaymentRequest request, CancellationToken cancellationToken)
    {
        return Respond(await customerService.ReceivePaymentAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/adjustments")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<BalanceEntryDto>>> Adjust(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken)
    {
        return Respond(await customerService.AdjustBalanceAsync(id, request, cancellationToken));
    }
}

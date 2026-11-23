using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/merchants")]
[RequirePermission(AdminArea.Merchants)]
public class AdminMerchantsController(IAdminMerchantService merchantService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AdminMerchantListItemDto>>>> GetAll([FromQuery] AdminMerchantQuery query, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.GetAsync(id, cancellationToken));
    }

    /// <summary>Creates the merchant, its owner login, a first branch, warehouse and register.</summary>
    [HttpPost]
    [RequirePermission(AdminArea.Merchants, AdminActions.Create)]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> Create(CreateMerchantRequest request, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [RequirePermission(AdminArea.Merchants, AdminActions.Update)]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> Update(int id, UpdateMerchantRequest request, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/status")]
    [RequirePermission(AdminArea.Merchants, AdminActions.Update)]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> UpdateStatus(int id, UpdateMerchantStatusRequest request, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.UpdateStatusAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/subscription")]
    [RequirePermission(AdminArea.Merchants, AdminActions.Update)]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> UpdateSubscription(int id, UpdateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.UpdateSubscriptionAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/users/{userId:int}/password")]
    [RequirePermission(AdminArea.Merchants, AdminActions.Update)]
    public async Task<ActionResult<Result>> ResetUserPassword(int id, int userId, ResetUserPasswordRequest request, CancellationToken cancellationToken)
    {
        return Respond(await merchantService.ResetUserPasswordAsync(id, userId, request, cancellationToken));
    }
}

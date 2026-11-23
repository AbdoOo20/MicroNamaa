using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.DTOs.MerchantApplications;

using MicroNamaa.Application.Services.MerchantApplications;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/merchant-applications")]
[RequirePermission(AdminArea.MerchantApplications)]
public class AdminMerchantApplicationsController(IMerchantApplicationService applicationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<MerchantApplicationDto>>>> GetAll([FromQuery] MerchantApplicationQuery query, CancellationToken cancellationToken)
    {
        return Respond(await applicationService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<MerchantApplicationDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await applicationService.GetAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/approve")]
    [RequirePermission(AdminArea.MerchantApplications, AdminActions.Update)]
    public async Task<ActionResult<Result<AdminMerchantDetailDto>>> Approve(int id, ApproveMerchantApplicationRequest request, CancellationToken cancellationToken)
    {
        return Respond(await applicationService.ApproveAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/reject")]
    [RequirePermission(AdminArea.MerchantApplications, AdminActions.Update)]
    public async Task<ActionResult<Result<MerchantApplicationDto>>> Reject(int id, RejectMerchantApplicationRequest request, CancellationToken cancellationToken)
    {
        return Respond(await applicationService.RejectAsync(id, request, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/overview")]
[RequirePermission(AdminArea.Overview)]
public class AdminOverviewController(IAdminMerchantService merchantService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<AdminOverviewDto>>> Get(CancellationToken cancellationToken)
    {
        return Respond(await merchantService.GetOverviewAsync(cancellationToken));
    }
}

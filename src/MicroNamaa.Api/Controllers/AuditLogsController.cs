using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Audit;

using MicroNamaa.Application.Services.Audit;

namespace MicroNamaa.Api.Controllers;

/// <summary>Who changed what in the store. The owner only.</summary>
[Authorize(Roles = AppRoles.Owner)]
[Route("api/audit-logs")]
public class AuditLogsController(IAuditLogService auditLogService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AuditLogDto>>>> GetAll([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        return Respond(await auditLogService.GetAllAsync(query, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Audit;

using MicroNamaa.Application.Services.Audit;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("api/admin/audit-logs")]
[RequirePermission(AdminArea.AuditLog)]
public class AdminAuditLogsController(IAuditLogService auditLogService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<AuditLogDto>>>> GetAll([FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        return Respond(await auditLogService.GetAllAsync(query, cancellationToken));
    }
}

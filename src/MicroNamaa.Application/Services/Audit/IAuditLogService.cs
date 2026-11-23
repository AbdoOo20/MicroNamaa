using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Audit;

namespace MicroNamaa.Application.Services.Audit;

public interface IAuditLogService
{
    /// <summary>Platform admins see every merchant; a merchant owner sees only the changes made in their store.</summary>
    Task<Result<PagedResult<AuditLogDto>>> GetAllAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}

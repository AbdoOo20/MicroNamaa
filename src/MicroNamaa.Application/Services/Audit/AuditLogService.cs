using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Audit;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Audit;

public class AuditLogService(IUnitOfWork unitOfWork, IUserAppService userAppService) : IAuditLogService
{
    public async Task<Result<PagedResult<AuditLogDto>>> GetAllAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var logs = unitOfWork.Repository<AuditLog>().Query();

        var merchantId = userAppService.IsAdmin ? query.MerchantId : userAppService.RequiredMerchantId;

        if (merchantId is { } merchant)
        {
            logs = logs.Where(log => log.MerchantId == merchant);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityName))
        {
            logs = logs.Where(log => log.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            logs = logs.Where(log => log.EntityId == query.EntityId);
        }

        if (query.UserId is { } userId)
        {
            logs = logs.Where(log => log.UserId == userId);
        }

        if (query.From is { } from)
        {
            logs = logs.Where(log => log.CreatedAt >= from);
        }

        if (query.To is { } to)
        {
            logs = logs.Where(log => log.CreatedAt < to);
        }

        var users = unitOfWork.Repository<User>().Query();

        var page = await logs
            .OrderByDescending(log => log.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(log => new AuditLogDto(log.Id, log.UserId, users.Where(user => user.Id == log.UserId).Select(user => user.FullName).FirstOrDefault(), log.MerchantId, log.Action, log.EntityName, log.EntityId, log.OldValues, log.NewValues, log.IpAddress, log.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<AuditLogDto>>.Ok(page);
    }
}

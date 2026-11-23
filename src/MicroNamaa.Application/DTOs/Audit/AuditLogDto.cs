namespace MicroNamaa.Application.DTOs.Audit;

public record AuditLogDto(long Id, int? UserId, string? UserName, int? MerchantId, string Action, string EntityName, string? EntityId, string? OldValues, string? NewValues, string? IpAddress, DateTime CreatedAt);

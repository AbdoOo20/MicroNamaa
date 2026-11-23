using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

public class AuditLog : BaseEntity<long>
{
    public int? UserId { get; set; }

    public int? MerchantId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }
}

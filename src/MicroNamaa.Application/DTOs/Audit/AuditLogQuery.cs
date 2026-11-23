using System.ComponentModel.DataAnnotations;

using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Audit;

public class AuditLogQuery : PagedQuery
{
    [MaxLength(100)]
    public string? EntityName { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    public int? UserId { get; set; }

    /// <summary>Platform admins only; a merchant always sees its own log.</summary>
    public int? MerchantId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}

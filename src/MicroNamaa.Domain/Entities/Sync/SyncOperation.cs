using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

/// <summary>
/// An operation a device did offline and sent with the sync push, kept once it was applied. The same OperationId sent again
/// is not applied twice, and ClientKey maps the device's key of a new record to its server id for later operations.
/// </summary>
public class SyncOperation : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public Guid OperationId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? ClientKey { get; set; }

    public string? EntityType { get; set; }

    public long? EntityId { get; set; }

    public string? Code { get; set; }

    public int StatusCode { get; set; }

    public string? Message { get; set; }

    /// <summary>When the device did it (UTC).</summary>
    public DateTime? OccurredAt { get; set; }
}

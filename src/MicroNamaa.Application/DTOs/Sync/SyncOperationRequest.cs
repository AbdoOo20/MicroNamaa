using System.ComponentModel.DataAnnotations;

using System.Text.Json;

namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>
/// One thing done on the device while offline, in the order it was done.
/// Any id inside <see cref="Payload" /> or <see cref="TargetId" /> can be "@clientKey" of a record created earlier offline
/// (in this push or a previous one); for a line of an offline invoice use "@invoiceKey#0", "@invoiceKey#1"... (line order).
/// </summary>
public record SyncOperationRequest
{
    /// <summary>A new GUID per operation, made on the device. Sending it again never applies it twice.</summary>
    public Guid OperationId { get; init; }

    /// <summary>For example customers.create, sales.invoice.create, purchases.deliver (see GET /api/sync/operations).</summary>
    [Required, MaxLength(50)]
    public string Type { get; init; } = string.Empty;

    /// <summary>The device's key of the record this operation creates (for example "cust-7f3a"), to reference it later as "@cust-7f3a".</summary>
    [MaxLength(100)]
    public string? ClientKey { get; init; }

    /// <summary>The record changed by an update, delete or action: a number, or "@clientKey".</summary>
    public JsonElement? TargetId { get; init; }

    /// <summary>The same body as the online endpoint of this operation.</summary>
    public JsonElement? Payload { get; init; }

    /// <summary>When it was done on the device (UTC). A sale with this time is recorded as an offline sale.</summary>
    public DateTime? OccurredAt { get; init; }
}

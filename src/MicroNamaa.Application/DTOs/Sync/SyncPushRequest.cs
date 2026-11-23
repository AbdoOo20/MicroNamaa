using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncPushRequest
{
    /// <summary>Stable id of the device, for the log.</summary>
    [MaxLength(100)]
    public string? DeviceId { get; init; }

    [Required, MinLength(1), MaxLength(200)]
    public IReadOnlyList<SyncOperationRequest> Operations { get; init; } = [];
}

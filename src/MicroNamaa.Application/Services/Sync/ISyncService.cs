using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sync;

namespace MicroNamaa.Application.Services.Sync;

/// <summary>
/// Offline work for the desktop app: pull the store's data to keep on the device, and push what was done offline.
/// </summary>
public interface ISyncService
{
    /// <summary>Everything (since empty) or what changed after <paramref name="since" />.</summary>
    Task<Result<SyncPullDto>> PullAsync(DateTime? since, CancellationToken cancellationToken = default);

    /// <summary>Applies the operations in order; each one succeeds or fails alone and is never applied twice.</summary>
    Task<Result<SyncPushResultDto>> PushAsync(SyncPushRequest request, CancellationToken cancellationToken = default);

    Result<IReadOnlyList<SyncOperationTypeDto>> GetOperationTypes();
}

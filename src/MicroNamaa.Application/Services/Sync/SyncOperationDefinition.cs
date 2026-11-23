using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.Services.Sync;

/// <summary>An operation type of the push: who may send it, what it creates, and the service call that does it.</summary>
public record SyncOperationDefinition(string Type, SyncAccess Access, string? EntityType, bool NeedsTargetId, string? Body, Func<IServiceProvider, SyncOperationContext, CancellationToken, Task<Result>> Run);

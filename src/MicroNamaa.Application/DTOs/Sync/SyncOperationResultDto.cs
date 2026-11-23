namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>
/// What happened to one operation. Status: applied (done now), duplicate (done before; same result returned),
/// failed (not done; fix it or drop it — retrying with the same OperationId is allowed), skipped (it used a record whose
/// creation failed in this push).
/// </summary>
public record SyncOperationResultDto(Guid OperationId, string Type, string? ClientKey, string Status, int StatusCode, string? Message, IReadOnlyList<string> Errors, string? EntityType, long? Id, string? Code);

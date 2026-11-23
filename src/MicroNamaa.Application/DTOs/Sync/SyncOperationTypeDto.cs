namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>An operation the push accepts. Access: Store (owner, manager, cashier), Managers, Owner.</summary>
public record SyncOperationTypeDto(string Type, string Access, string? EntityType, bool NeedsTargetId, string? Body);

namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>The result of every operation in order, and the server id of every client key created (KeyMap).</summary>
public record SyncPushResultDto(int Applied, int Duplicates, int Failed, IReadOnlyList<SyncOperationResultDto> Results, IReadOnlyDictionary<string, long> KeyMap, DateTime ServerTime);

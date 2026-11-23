using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncWarehouseDto(int Id, string Name, string? Location, int? BranchId, int? Capacity, RecordStatus Status, DateTime UpdatedAt);

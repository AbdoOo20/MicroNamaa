using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

/// <summary>CapacityPercent = units in stock / capacity (empty when the capacity is not set).</summary>
public record WarehouseDto(int Id, string Name, string? Location, int? BranchId, string? BranchName, int? Capacity, RecordStatus Status, int ProductsCount, int TotalUnits, decimal? CapacityPercent, decimal StockValue, DateTime CreatedAt);

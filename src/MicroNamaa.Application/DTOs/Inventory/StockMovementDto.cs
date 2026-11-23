using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockMovementDto(long Id, int ProductId, string ProductName, string Sku, int WarehouseId, string WarehouseName, StockMovementType Type, int Quantity, int BalanceAfter, decimal UnitCost, string? ReferenceType, long? ReferenceId, string? ReferenceCode, string? Note, string? UserName, DateTime CreatedAt);

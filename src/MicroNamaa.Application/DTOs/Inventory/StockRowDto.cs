using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockRowDto(int ProductId, string ProductName, string Sku, string? ImageUrl, int WarehouseId, string WarehouseName, int Quantity, decimal Cost, decimal StockValue, StockState StockState);

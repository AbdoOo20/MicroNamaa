namespace MicroNamaa.Application.DTOs.Reports;

public record WarehouseStockDto(int WarehouseId, string Name, string? Location, int ProductsCount, int Units, decimal StockValue, decimal RetailValue, decimal? CapacityPercent);

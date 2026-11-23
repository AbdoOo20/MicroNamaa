using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Inventory;

/// <summary>The cards of the warehouses screen: active warehouses, products in stock, and the average capacity used.</summary>
public record WarehouseSummaryDto(int ActiveWarehouses, int TotalProducts, int TotalUnits, decimal? AverageCapacity, IReadOnlyList<SummaryCount> Tabs);

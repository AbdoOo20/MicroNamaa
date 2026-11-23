using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Catalog;

/// <summary>The cards and tabs of the products screen. TotalCost is the stock on hand at cost.</summary>
public record ProductSummaryDto(int TotalProducts, decimal TotalCost, decimal TotalRetailValue, int LowStock, int OutOfStock, IReadOnlyList<SummaryCount> Tabs);

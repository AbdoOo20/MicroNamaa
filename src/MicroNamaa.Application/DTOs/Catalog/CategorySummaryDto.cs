using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Catalog;

/// <summary>The cards of the categories screen. TotalCost is the stock on hand at cost.</summary>
public record CategorySummaryDto(int TotalCategories, int TotalProducts, decimal TotalCost, IReadOnlyList<SummaryCount> Tabs);

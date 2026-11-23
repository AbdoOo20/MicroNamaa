namespace MicroNamaa.Application.DTOs.Catalog;

/// <summary>The cards and charts of the product details screen. "This month" follows Libyan time.</summary>
public record ProductPerformanceDto(
    int UnitsSoldThisMonth,
    decimal RevenueThisMonth,
    decimal? MonthlyDemandPercent,
    int CategoryUnitsThisMonth,
    decimal? GrossMarginPercent,
    decimal GrossProfitThisMonth,
    IReadOnlyList<MonthlyUnitsDto> Trend,
    IReadOnlyList<DemandShareDto> DemandShare);

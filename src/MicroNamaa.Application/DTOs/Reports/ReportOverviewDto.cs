namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>
/// The period's numbers, net of returns, with the change against the period of the same length just before it.
/// GrossProfit = sales without tax - cost of the goods sold.
/// </summary>
public record ReportOverviewDto(
    DateTime From,
    DateTime To,
    decimal Revenue,
    decimal? RevenueGrowth,
    int Orders,
    decimal? OrdersGrowth,
    decimal AverageOrderValue,
    decimal? AverageOrderGrowth,
    int ItemsSold,
    decimal Returns,
    decimal Discounts,
    decimal Tax,
    decimal GrossProfit,
    decimal? GrossProfitGrowth,
    decimal? GrossMarginPercent,
    decimal UnpaidAmount,
    int NewCustomers);

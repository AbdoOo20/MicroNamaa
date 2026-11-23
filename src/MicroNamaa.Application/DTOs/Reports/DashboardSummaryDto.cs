namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>The four cards of the dashboard. SalesGrowthPercent compares this month so far with the same days of last month.</summary>
public record DashboardSummaryDto(decimal SalesThisMonth, decimal? SalesGrowthPercent, int OrdersThisMonth, int TotalProducts, int ProductsAddedThisMonth, int Customers, int Warehouses, int OutOfStock, int LowStock, int AwaitingDelivery, string Currency);

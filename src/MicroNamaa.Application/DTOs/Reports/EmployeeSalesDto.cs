namespace MicroNamaa.Application.DTOs.Reports;

/// <summary>Sales per cashier. Performance compares this month's sales with the monthly target.</summary>
public record EmployeeSalesDto(int UserId, string FullName, string? AvatarUrl, string? BranchName, int Orders, decimal Revenue, decimal SalesThisMonth, decimal? MonthlyTarget, decimal? PerformancePercent);

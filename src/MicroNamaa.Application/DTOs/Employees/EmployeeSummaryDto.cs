using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Employees;

public record EmployeeSummaryDto(int TotalEmployees, int ActiveAccounts, decimal? AveragePerformance, IReadOnlyList<SummaryCount> Tabs);

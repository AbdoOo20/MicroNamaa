namespace MicroNamaa.Application.DTOs.Employees;

/// <summary>A merchant's user. Performance = this month's sales / monthly target, in percent.</summary>
public record EmployeeDto(int Id, string FullName, string? Email, string? Phone, string? AvatarUrl, string Role, string? JobTitle, int? BranchId, string? BranchName, decimal? MonthlyTarget, decimal SalesThisMonth, decimal? PerformancePercent, bool IsActive, DateTime? LastLoginAt, DateTime CreatedAt);

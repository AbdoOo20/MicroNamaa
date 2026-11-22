using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Employees;

public record UpdateEmployeeRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [Required, MaxLength(50)]
    public string Role { get; init; } = "Cashier";

    [MaxLength(100)]
    public string? JobTitle { get; init; }

    public int? BranchId { get; init; }

    [Range(0, 100000000)]
    public decimal? MonthlyTarget { get; init; }

    public bool IsActive { get; init; } = true;
}

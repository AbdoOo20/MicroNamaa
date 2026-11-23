using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Employees;

/// <summary>An employee signs in with their mobile number or email (at least one) and this password.</summary>
public record CreateEmployeeRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; init; } = string.Empty;

    /// <summary>MerchantOwner, MerchantManager or Cashier. Managers can only add cashiers.</summary>
    [Required, MaxLength(50)]
    public string Role { get; init; } = "Cashier";

    [MaxLength(100)]
    public string? JobTitle { get; init; }

    /// <summary>Required for cashiers.</summary>
    public int? BranchId { get; init; }

    [Range(0, 100000000)]
    public decimal? MonthlyTarget { get; init; }
}

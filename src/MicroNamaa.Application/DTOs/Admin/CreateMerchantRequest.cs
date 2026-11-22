using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Admin;

/// <summary>
/// A new merchant with its owner's login (mobile or email + password), a first branch with a warehouse and a register.
/// </summary>
public record CreateMerchantRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string OwnerName { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [Required, MinLength(8), MaxLength(100)]
    public string OwnerPassword { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(50)]
    public string? TaxNumber { get; init; }

    /// <summary>Libyan date the subscription ends (the whole day included); Platform:DefaultSubscriptionDays from today when empty.</summary>
    public DateOnly? SubscriptionEndsOn { get; init; }

    [Range(1, 1000)]
    public int? MaxBranches { get; init; }

    [Range(1, 10000)]
    public int? MaxUsers { get; init; }

    [Range(0, 100)]
    public decimal TaxRate { get; init; }

    [MaxLength(150)]
    public string? BranchName { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

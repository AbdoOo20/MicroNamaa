using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.MerchantApplications;

public record SubmitMerchantApplicationRequest
{
    [Required, MaxLength(150)]
    public string BusinessName { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string OwnerName { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    /// <summary>Supermarket, pharmacy, clothes...</summary>
    [MaxLength(100)]
    public string? BusinessType { get; init; }

    [Range(1, 1000)]
    public int? BranchesCount { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

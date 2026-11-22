using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.MerchantApplications;

/// <summary>Creates the merchant from the request. The owner signs in with the request's mobile number and this password.</summary>
public record ApproveMerchantApplicationRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string OwnerPassword { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    public DateOnly? SubscriptionEndsOn { get; init; }

    [Range(1, 1000)]
    public int? MaxBranches { get; init; }

    [Range(1, 10000)]
    public int? MaxUsers { get; init; }

    [MaxLength(500)]
    public string? AdminNote { get; init; }
}

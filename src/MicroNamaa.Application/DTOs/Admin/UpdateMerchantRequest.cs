using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Admin;

public record UpdateMerchantRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string OwnerName { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(50)]
    public string? TaxNumber { get; init; }

    [Range(1, 1000)]
    public int? MaxBranches { get; init; }

    [Range(1, 10000)]
    public int? MaxUsers { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record CustomerRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; init; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    public int? BranchId { get; init; }

    [Range(0, 100000000)]
    public decimal? CreditLimit { get; init; }

    public RecordStatus Status { get; init; } = RecordStatus.Active;

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

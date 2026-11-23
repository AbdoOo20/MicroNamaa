using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record SupplierRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(150)]
    public string? ContactPerson { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(50)]
    public string? TaxNumber { get; init; }

    public RecordStatus Status { get; init; } = RecordStatus.Active;

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

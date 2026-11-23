using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Merchants;

public record BranchRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    /// <summary>The warehouse the branch's POS sells from.</summary>
    public int? DefaultWarehouseId { get; init; }

    /// <summary>On create only: also make a warehouse for the branch and sell from it.</summary>
    public bool CreateWarehouse { get; init; }

    public bool IsActive { get; init; } = true;
}

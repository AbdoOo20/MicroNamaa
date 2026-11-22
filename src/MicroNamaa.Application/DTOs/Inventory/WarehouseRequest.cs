using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public record WarehouseRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(300)]
    public string? Location { get; init; }

    public int? BranchId { get; init; }

    [Range(1, 100000000)]
    public int? Capacity { get; init; }

    public RecordStatus Status { get; init; } = RecordStatus.Active;
}

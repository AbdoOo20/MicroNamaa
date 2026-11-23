using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public record CategoryRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; init; }

    public RecordStatus Status { get; init; } = RecordStatus.Active;

    [Range(0, 10000)]
    public int SortOrder { get; init; }
}

using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public record ProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Left empty, a SKU is made from the product number.</summary>
    [MaxLength(50)]
    public string? Sku { get; init; }

    [MaxLength(50)]
    public string? Barcode { get; init; }

    public int? CategoryId { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Range(0, 100000000)]
    public decimal Cost { get; init; }

    [Range(0, 100000000)]
    public decimal Price { get; init; }

    [Range(0, 1000000)]
    public int? LowStockThreshold { get; init; }

    public RecordStatus Status { get; init; } = RecordStatus.Active;
}

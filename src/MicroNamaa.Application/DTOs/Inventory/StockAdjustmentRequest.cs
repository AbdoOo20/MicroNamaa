using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockAdjustmentRequest
{
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; init; }

    [Required, MinLength(1), MaxLength(500)]
    public IReadOnlyList<StockCountLine> Items { get; init; } = [];

    [MaxLength(500)]
    public string? Note { get; init; }
}

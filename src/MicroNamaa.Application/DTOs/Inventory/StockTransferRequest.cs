using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockTransferRequest
{
    [Range(1, int.MaxValue)]
    public int FromWarehouseId { get; init; }

    [Range(1, int.MaxValue)]
    public int ToWarehouseId { get; init; }

    [Required, MinLength(1), MaxLength(500)]
    public IReadOnlyList<StockTransferLine> Items { get; init; } = [];

    [MaxLength(500)]
    public string? Note { get; init; }
}

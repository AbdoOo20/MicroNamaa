using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockCountLine
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    /// <summary>What was counted on the shelf; the difference becomes an adjustment.</summary>
    [Range(0, 10000000)]
    public int CountedQuantity { get; init; }
}

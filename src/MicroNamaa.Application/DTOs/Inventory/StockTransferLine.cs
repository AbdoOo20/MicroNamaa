using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Inventory;

public record StockTransferLine
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10000000)]
    public int Quantity { get; init; }
}

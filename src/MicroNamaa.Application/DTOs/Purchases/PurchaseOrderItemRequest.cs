using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Purchases;

public record PurchaseOrderItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 10000000)]
    public int Quantity { get; init; } = 1;

    [Range(0, 100000000)]
    public decimal UnitCost { get; init; }
}

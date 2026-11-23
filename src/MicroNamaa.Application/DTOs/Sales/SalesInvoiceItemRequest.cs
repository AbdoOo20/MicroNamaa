using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Sales;

public record SalesInvoiceItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 100000)]
    public int Quantity { get; init; } = 1;

    /// <summary>Offline sync only: the unit price the customer paid on the device. Ignored by the online endpoints.</summary>
    [Range(0, 100000000)]
    public decimal? UnitPrice { get; init; }
}

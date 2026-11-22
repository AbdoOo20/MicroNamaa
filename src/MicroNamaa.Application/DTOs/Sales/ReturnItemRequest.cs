using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Sales;

public record ReturnItemRequest
{
    [Range(1, long.MaxValue)]
    public long SalesInvoiceItemId { get; init; }

    [Range(1, 100000)]
    public int Quantity { get; init; } = 1;

    /// <summary>False for damaged goods that do not go back on the shelf.</summary>
    public bool Restock { get; init; } = true;
}

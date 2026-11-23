using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Sales;

public record SalesInvoiceItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, 100000)]
    public int Quantity { get; init; } = 1;
}

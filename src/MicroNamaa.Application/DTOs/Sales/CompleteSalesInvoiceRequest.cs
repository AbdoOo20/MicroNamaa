using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Sales;

public record CompleteSalesInvoiceRequest
{
    [Required]
    public InvoicePaymentRequest Payment { get; init; } = new();
}

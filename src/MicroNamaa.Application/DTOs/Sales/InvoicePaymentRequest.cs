using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

/// <summary>
/// Cash: TenderedAmount is what the customer handed over (change = tendered - total).
/// Card / bank transfer: paid in full. Deferred: a customer is required and PaidAmount (0 by default) is paid now, the rest goes to their balance.
/// </summary>
public record InvoicePaymentRequest
{
    public PaymentMethod Method { get; init; } = PaymentMethod.Cash;

    public decimal? PaidAmount { get; init; }

    public decimal? TenderedAmount { get; init; }
}

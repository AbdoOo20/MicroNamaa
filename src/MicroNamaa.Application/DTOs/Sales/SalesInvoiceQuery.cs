using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public class SalesInvoiceQuery : PagedQuery
{
    public SalesInvoiceStatus? Status { get; set; }

    public int? BranchId { get; set; }

    public int? CustomerId { get; set; }

    public int? CashierId { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Only invoices with an unpaid part.</summary>
    public bool UnpaidOnly { get; set; }

    /// <summary>Libyan date, included.</summary>
    public DateOnly? From { get; set; }

    /// <summary>Libyan date, included.</summary>
    public DateOnly? To { get; set; }
}

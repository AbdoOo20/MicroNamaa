using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sales;

public class SalesReturnQuery : PagedQuery
{
    public int? BranchId { get; set; }

    public int? CustomerId { get; set; }

    public long? SalesInvoiceId { get; set; }

    public RefundMethod? RefundMethod { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }
}

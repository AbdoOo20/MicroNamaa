using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A line of the customer's statement. Positive amounts add to what the customer owes.</summary>
public class CustomerBalanceEntry : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public int CustomerId { get; set; }

    public BalanceEntryType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public long? SalesInvoiceId { get; set; }

    public long? SalesReturnId { get; set; }

    public string? Reference { get; set; }

    public string? Note { get; set; }

    public Customer Customer { get; set; } = null!;
}

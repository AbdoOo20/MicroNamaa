using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A line of the supplier's statement. Positive amounts add to what the merchant owes the supplier.</summary>
public class SupplierBalanceEntry : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public int SupplierId { get; set; }

    public BalanceEntryType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAfter { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public long? PurchaseOrderId { get; set; }

    public string? Reference { get; set; }

    public string? Note { get; set; }

    public Supplier Supplier { get; set; } = null!;
}

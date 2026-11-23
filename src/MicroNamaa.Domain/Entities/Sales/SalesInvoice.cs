using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>
/// A POS sale. Amounts: Total = Subtotal - ItemsDiscount - OrderDiscount - PointsDiscount + TaxAmount;
/// DueAmount = Total - PaidAmount goes to the customer's balance (deferred sales).
/// </summary>
public class SalesInvoice : BaseEntity<long>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public long Number { get; set; }

    public int BranchId { get; set; }

    public int? RegisterId { get; set; }

    public int WarehouseId { get; set; }

    public int? CustomerId { get; set; }

    public int CashierId { get; set; }

    public SalesInvoiceStatus Status { get; set; } = SalesInvoiceStatus.Draft;

    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>Quantity × unit price of every line, before any discount.</summary>
    public decimal Subtotal { get; set; }

    public decimal ItemsDiscount { get; set; }

    public int? PromotionId { get; set; }

    public string? PromotionName { get; set; }

    /// <summary>A discount the cashier typed for the whole invoice, on top of the promotion.</summary>
    public DiscountType? ManualDiscountType { get; set; }

    public decimal? ManualDiscountValue { get; set; }

    /// <summary>The promotion's discount plus the manual discount.</summary>
    public decimal OrderDiscount { get; set; }

    public int PointsRedeemed { get; set; }

    public decimal PointsDiscount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public decimal PaidAmount { get; set; }

    /// <summary>The cash the customer handed over; ChangeAmount = Tendered - Paid.</summary>
    public decimal? TenderedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    public decimal DueAmount { get; set; }

    public int PointsEarned { get; set; }

    /// <summary>Cost of the goods sold, for the profit reports.</summary>
    public decimal TotalCost { get; set; }

    public decimal ReturnedAmount { get; set; }

    public string? Note { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int? CancelledBy { get; set; }

    public string? CancelReason { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public string Code => FormatCode(Number);

    public Branch Branch { get; set; } = null!;

    public Register? Register { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Customer? Customer { get; set; }

    public User Cashier { get; set; } = null!;

    public ICollection<SalesInvoiceItem> Items { get; set; } = [];

    public static string FormatCode(long number) => $"INV-{number:000000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Number.ToString(), Note);
}

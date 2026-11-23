using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>Goods bought from a supplier. Stock, cost and the supplier's balance change only when it is delivered.</summary>
public class PurchaseOrder : BaseEntity<long>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public long Number { get; set; }

    public int SupplierId { get; set; }

    public int WarehouseId { get; set; }

    public DateTime OrderDate { get; set; }

    public DateTime? ExpectedAt { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    /// <summary>Net 15, Net 30, due on receipt...</summary>
    public string? PaymentTerms { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxRate { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal ShippingCost { get; set; }

    public decimal Total { get; set; }

    /// <summary>Paid to the supplier when it was delivered; the rest goes to the supplier's balance.</summary>
    public decimal PaidAmount { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public string? Note { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancelReason { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public string Code => FormatCode(Number);

    public Supplier Supplier { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<PurchaseOrderItem> Items { get; set; } = [];

    public static string FormatCode(long number) => $"PO-{number:000000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Number.ToString(), PaymentTerms, Note);
}

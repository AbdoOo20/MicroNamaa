using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

public class SalesInvoiceItem : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public long SalesInvoiceId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal UnitCost { get; set; }

    public decimal DiscountAmount { get; set; }

    public int? PromotionId { get; set; }

    public string? PromotionName { get; set; }

    /// <summary>Quantity × UnitPrice - DiscountAmount.</summary>
    public decimal LineTotal { get; set; }

    public int ReturnedQuantity { get; set; }

    public SalesInvoice SalesInvoice { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

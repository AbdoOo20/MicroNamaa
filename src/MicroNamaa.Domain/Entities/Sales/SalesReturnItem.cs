using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

public class SalesReturnItem : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public long SalesReturnId { get; set; }

    public long SalesInvoiceItemId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    /// <summary>The refunded amount per unit: the line's price after every discount, with tax.</summary>
    public decimal UnitRefund { get; set; }

    public decimal Total { get; set; }

    /// <summary>False for damaged goods that do not go back to stock.</summary>
    public bool Restock { get; set; } = true;

    public SalesReturn SalesReturn { get; set; } = null!;

    public SalesInvoiceItem SalesInvoiceItem { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

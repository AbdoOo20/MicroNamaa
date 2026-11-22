using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

public class PurchaseOrderItem : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public long PurchaseOrderId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Total { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

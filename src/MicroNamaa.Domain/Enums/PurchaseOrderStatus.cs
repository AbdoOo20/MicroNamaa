namespace MicroNamaa.Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft = 1,

    Pending = 2,

    InTransit = 3,

    /// <summary>Received into the warehouse: stock, product cost and the supplier's balance were updated.</summary>
    Delivered = 4,

    Cancelled = 5
}

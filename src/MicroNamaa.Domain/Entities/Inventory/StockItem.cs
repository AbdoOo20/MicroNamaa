using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

/// <summary>How many units of a product are in a warehouse. Only <c>StockLedger</c> changes it.</summary>
public class StockItem : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Warehouse Warehouse { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>One change of stock (sale, purchase, return, adjustment, transfer) with the balance after it.</summary>
public class StockMovement : BaseEntity<long>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>Positive adds stock, negative takes it.</summary>
    public int Quantity { get; set; }

    public int BalanceAfter { get; set; }

    public decimal UnitCost { get; set; }

    public string? ReferenceType { get; set; }

    public long? ReferenceId { get; set; }

    public string? ReferenceCode { get; set; }

    public string? Note { get; set; }

    public Warehouse Warehouse { get; set; } = null!;

    public Product Product { get; set; } = null!;
}

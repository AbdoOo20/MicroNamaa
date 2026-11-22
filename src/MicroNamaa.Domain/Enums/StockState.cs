namespace MicroNamaa.Domain.Enums;

/// <summary>Worked out from the quantity and the product's low-stock threshold; never stored.</summary>
public enum StockState
{
    InStock = 1,

    LowStock = 2,

    OutOfStock = 3
}

using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A product of the merchant's catalog. Its stock is the sum of <see cref="StockItem" /> rows in the warehouses.</summary>
public class Product : BaseEntity<int>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public int? CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? Barcode { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    /// <summary>Average cost per unit (LYD); recalculated when a purchase is received.</summary>
    public decimal Cost { get; set; }

    /// <summary>Selling price per unit (LYD), before tax.</summary>
    public decimal Price { get; set; }

    /// <summary>Low stock below or at this quantity; empty uses the merchant's default.</summary>
    public int? LowStockThreshold { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public string SearchText { get; set; } = string.Empty;

    public Category? Category { get; set; }

    public ICollection<StockItem> Stock { get; set; } = [];

    public string BuildSearchText() => ArabicSearch.Build(Name, Sku, Barcode);
}

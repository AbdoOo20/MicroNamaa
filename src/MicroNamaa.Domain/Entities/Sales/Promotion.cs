using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Promotion : BaseEntity<int>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DiscountType Type { get; set; }

    /// <summary>The percent (5 = 5%) or the LYD amount (per unit for product and category promotions).</summary>
    public decimal Value { get; set; }

    public PromotionScope Scope { get; set; }

    public int? ProductId { get; set; }

    public int? CategoryId { get; set; }

    /// <summary>For order promotions: the subtotal the invoice must reach.</summary>
    public decimal? MinOrderAmount { get; set; }

    public DateTime? StartsAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public bool IsActive { get; set; } = true;

    public Product? Product { get; set; }

    public Category? Category { get; set; }
}

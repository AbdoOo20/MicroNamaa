namespace MicroNamaa.Domain.Enums;

public enum PromotionScope
{
    /// <summary>Chosen by the cashier for the whole invoice (for example a VIP promo).</summary>
    Order = 1,

    /// <summary>Applied automatically to one product's lines.</summary>
    Product = 2,

    /// <summary>Applied automatically to every product of a category.</summary>
    Category = 3
}

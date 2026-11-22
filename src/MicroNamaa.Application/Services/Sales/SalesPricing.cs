using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Sales;

/// <summary>
/// Prices a POS invoice: each line gets its best product or category promotion, then the order promotion and the
/// cashier's discount, then the loyalty points, then the tax on what is left. Total = Subtotal - discounts + tax.
/// </summary>
public static class SalesPricing
{
    public static PricingResult Calculate(PricingInput input)
    {
        var lines = input.Lines.Select(line => PriceLine(line.Product, line.Quantity, input.ItemPromotions)).ToList();

        var subtotal = Money.Round(lines.Sum(line => line.UnitPrice * line.Quantity));

        var itemsDiscount = Money.Round(lines.Sum(line => line.Discount));

        var net = subtotal - itemsDiscount;

        var promotionDiscount = 0m;

        if (input.OrderPromotion is { } promotion && net >= (promotion.MinOrderAmount ?? 0))
        {
            promotionDiscount = Discount(promotion.Type, promotion.Value, net);
        }

        var manualDiscount = input.ManualDiscountType is { } manualType && input.ManualDiscountValue is > 0
            ? Discount(manualType, input.ManualDiscountValue.Value, net - promotionDiscount)
            : 0m;

        var orderDiscount = Money.Round(promotionDiscount + manualDiscount);

        net -= orderDiscount;

        // Never spend more points than the invoice needs.
        var pointsRedeemed = input.PointValue <= 0 || net <= 0 ? 0 : Math.Min(input.PointsToRedeem, (int)Math.Ceiling(net / input.PointValue));

        var pointsDiscount = Money.Round(Math.Min(pointsRedeemed * input.PointValue, Math.Max(net, 0)));

        net -= pointsDiscount;

        var tax = Money.Percent(Math.Max(net, 0), input.TaxRate);

        var total = Money.Round(Math.Max(net, 0) + tax);

        var earned = input.EarnsPoints && input.PointsPerCurrencyUnit > 0 ? (int)Math.Floor(total * input.PointsPerCurrencyUnit) : 0;

        return new PricingResult(
            lines,
            subtotal,
            itemsDiscount,
            promotionDiscount > 0 ? input.OrderPromotion!.Id : null,
            promotionDiscount > 0 ? input.OrderPromotion!.Name : null,
            orderDiscount,
            pointsRedeemed,
            pointsDiscount,
            input.TaxRate,
            tax,
            total,
            earned);
    }

    /// <summary>The discount of one promotion on one amount, never more than the amount.</summary>
    public static decimal Discount(DiscountType type, decimal value, decimal amount)
    {
        if (amount <= 0 || value <= 0)
        {
            return 0;
        }

        var discount = type == DiscountType.Percentage ? amount * Math.Min(value, 100) / 100m : value;

        return Money.Round(Math.Min(discount, amount));
    }

    private static PricedLine PriceLine(Product product, int quantity, IReadOnlyList<Promotion> promotions)
    {
        var best = promotions
            .Where(promotion => (promotion.Scope == PromotionScope.Product && promotion.ProductId == product.Id) || (promotion.Scope == PromotionScope.Category && product.CategoryId != null && promotion.CategoryId == product.CategoryId))
            .Select(promotion => (Promotion: promotion, PerUnit: Discount(promotion.Type, promotion.Value, product.Price)))
            .Where(candidate => candidate.PerUnit > 0)
            .OrderByDescending(candidate => candidate.PerUnit)
            .FirstOrDefault();

        var discount = best.Promotion is null ? 0 : Money.Round(best.PerUnit * quantity);

        var lineTotal = Money.Round(product.Price * quantity - discount);

        return new PricedLine(product.Id, product.Name, product.Sku, quantity, product.Price, product.Cost, discount, best.Promotion?.Id, best.Promotion?.Name, lineTotal);
    }
}

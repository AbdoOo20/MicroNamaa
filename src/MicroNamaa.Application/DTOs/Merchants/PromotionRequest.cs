using System.ComponentModel.DataAnnotations;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Merchants;

public record PromotionRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    public DiscountType Type { get; init; } = DiscountType.Percentage;

    [Range(0.001, 1000000)]
    public decimal Value { get; init; }

    public PromotionScope Scope { get; init; } = PromotionScope.Order;

    public int? ProductId { get; init; }

    public int? CategoryId { get; init; }

    [Range(0, 100000000)]
    public decimal? MinOrderAmount { get; init; }

    public DateTime? StartsAt { get; init; }

    public DateTime? EndsAt { get; init; }

    public bool IsActive { get; init; } = true;
}

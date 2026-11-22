using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Merchants;

public record UpdateMerchantSettingsRequest
{
    [Required, MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    [Required, MaxLength(150)]
    public string OwnerName { get; init; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(300)]
    public string? Address { get; init; }

    [MaxLength(50)]
    public string? TaxNumber { get; init; }

    [Range(0, 100)]
    public decimal TaxRate { get; init; }

    public bool AllowNegativeStock { get; init; }

    [Range(0, 100000)]
    public int DefaultLowStockThreshold { get; init; } = 10;

    public bool LoyaltyEnabled { get; init; }

    [Range(0, 1000)]
    public decimal PointsPerCurrencyUnit { get; init; }

    [Range(0, 1000)]
    public decimal PointValue { get; init; }

    [Range(0, 1000000)]
    public int MinPointsToRedeem { get; init; }

    [MaxLength(500)]
    public string? ReceiptFooter { get; init; }
}

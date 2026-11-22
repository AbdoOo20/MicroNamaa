using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A business that bought the system (the tenant). Every store row carries its <c>MerchantId</c>.</summary>
public class Merchant : BaseEntity<int>, ISearchable
{
    public int Number { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? LogoPath { get; set; }

    public MerchantStatus Status { get; set; } = MerchantStatus.Active;

    /// <summary>The users cannot sign in after this moment (UTC). Empty means no end date.</summary>
    public DateTime? SubscriptionEndsAt { get; set; }

    public DateTime? SubscriptionReminderSentAt { get; set; }

    public int? MaxBranches { get; set; }

    public int? MaxUsers { get; set; }

    public string? Notes { get; set; }

    /// <summary>VAT / commercial tax in percent, added after discounts (5 = 5%).</summary>
    public decimal TaxRate { get; set; }

    /// <summary>Lets a sale go through when the warehouse shows less stock than sold.</summary>
    public bool AllowNegativeStock { get; set; }

    public int DefaultLowStockThreshold { get; set; } = 10;

    public bool LoyaltyEnabled { get; set; }

    /// <summary>Points earned for every 1 LYD of the invoice total.</summary>
    public decimal PointsPerCurrencyUnit { get; set; }

    /// <summary>How many LYD one point is worth when redeemed.</summary>
    public decimal PointValue { get; set; }

    public int MinPointsToRedeem { get; set; }

    public string? ReceiptFooter { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public string Code => FormatCode(Number);

    public ICollection<User> Users { get; set; } = [];

    public ICollection<Branch> Branches { get; set; } = [];

    public static string FormatCode(int number) => $"M-{number:000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Name, OwnerName, Phone, Email, City);
}

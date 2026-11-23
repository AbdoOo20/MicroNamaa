using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Customer : BaseEntity<int>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public long Number { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? AvatarPath { get; set; }

    /// <summary>The branch the customer usually buys from.</summary>
    public int? BranchId { get; set; }

    /// <summary>What the customer owes (deferred sales minus payments). Negative is credit in the customer's favour.</summary>
    public decimal Balance { get; set; }

    /// <summary>The most the customer may owe; empty means no limit.</summary>
    public decimal? CreditLimit { get; set; }

    public int LoyaltyPoints { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public string? Notes { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public string Code => FormatCode(Number);

    public Branch? Branch { get; set; }

    public static string FormatCode(long number) => $"C-{number:0000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Name, Phone, Email, City);
}

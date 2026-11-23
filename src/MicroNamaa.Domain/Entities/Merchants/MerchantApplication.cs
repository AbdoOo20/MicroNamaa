using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>A business asking to buy the system, sent from the landing page. The admin follows up and approves.</summary>
public class MerchantApplication : BaseEntity<int>, ISearchable
{
    public string BusinessName { get; set; } = string.Empty;

    public string OwnerName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? City { get; set; }

    public string? BusinessType { get; set; }

    public int? BranchesCount { get; set; }

    public string? Notes { get; set; }

    public MerchantApplicationStatus Status { get; set; } = MerchantApplicationStatus.Pending;

    public string? AdminNote { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedBy { get; set; }

    public int? MerchantId { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public string BuildSearchText() => ArabicSearch.Build(BusinessName, OwnerName, Phone, Email, City);
}

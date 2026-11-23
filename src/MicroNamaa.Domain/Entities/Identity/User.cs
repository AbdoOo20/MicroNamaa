using Microsoft.AspNetCore.Identity;

using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

/// <summary>
/// Anyone who signs in: a platform admin (no merchant), or a merchant's owner, manager or cashier (an employee).
/// </summary>
public class User : IdentityUser<int>, IAuditable, ISoftDeletable, ISearchable
{
    public string FullName { get; set; } = string.Empty;

    public int? MerchantId { get; set; }

    /// <summary>The branch a cashier sells from. Owners and managers may leave it empty.</summary>
    public int? BranchId { get; set; }

    /// <summary>Shown on the employees screen (Senior Sales Rep, Account Manager...).</summary>
    public string? JobTitle { get; set; }

    /// <summary>Monthly sales target in LYD; performance = this month's sales / target.</summary>
    public decimal? MonthlyTarget { get; set; }

    public string? AvatarPath { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>A sub-admin: a platform admin limited to the screens in <see cref="StaffPermissions"/>. The owner admin is not staff.</summary>
    public bool IsStaff { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public Merchant? Merchant { get; set; }

    public Branch? Branch { get; set; }

    public ICollection<StaffPermission> StaffPermissions { get; set; } = [];

    public string BuildSearchText() => ArabicSearch.Build(FullName, PhoneNumber, Email, JobTitle);
}

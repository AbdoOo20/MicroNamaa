using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Admin;

public class AdminMerchantQuery : PagedQuery
{
    public MerchantStatus? Status { get; set; }

    /// <summary>Subscriptions that end within this many days (already ended ones included).</summary>
    public int? ExpiringWithinDays { get; set; }
}

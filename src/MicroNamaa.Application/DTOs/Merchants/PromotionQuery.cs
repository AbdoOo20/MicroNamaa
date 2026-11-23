using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Merchants;

public class PromotionQuery : PagedQuery
{
    public PromotionScope? Scope { get; set; }

    /// <summary>Only promotions that apply right now.</summary>
    public bool RunningOnly { get; set; }
}

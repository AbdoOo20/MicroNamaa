using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Support;

public class TicketQuery : PagedQuery
{
    public TicketStatus? Status { get; set; }

    /// <summary>Admins only: the tickets of one merchant.</summary>
    public int? MerchantId { get; set; }
}

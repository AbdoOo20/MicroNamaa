using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public class BalanceEntryQuery : PagedQuery
{
    public BalanceEntryType? Type { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }
}

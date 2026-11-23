using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public class SupplierQuery : PagedQuery
{
    public RecordStatus? Status { get; set; }

    /// <summary>Only suppliers the merchant still owes.</summary>
    public bool OwedOnly { get; set; }
}

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public class CustomerQuery : PagedQuery
{
    public RecordStatus? Status { get; set; }

    public int? BranchId { get; set; }

    /// <summary>Only customers who owe money.</summary>
    public bool OwingOnly { get; set; }
}

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public class WarehouseQuery : PagedQuery
{
    public RecordStatus? Status { get; set; }

    public int? BranchId { get; set; }
}

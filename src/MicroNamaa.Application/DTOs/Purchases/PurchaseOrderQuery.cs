using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Purchases;

public class PurchaseOrderQuery : PagedQuery
{
    public PurchaseOrderStatus? Status { get; set; }

    public int? SupplierId { get; set; }

    public int? WarehouseId { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }
}

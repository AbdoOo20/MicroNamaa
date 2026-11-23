using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Inventory;

public class StockQuery : PagedQuery
{
    public int? WarehouseId { get; set; }

    public int? ProductId { get; set; }

    public StockState? StockState { get; set; }
}

using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Sales;

public class PosProductQuery : PagedQuery
{
    public int? BranchId { get; set; }

    public int? CategoryId { get; set; }

    public bool InStockOnly { get; set; }
}

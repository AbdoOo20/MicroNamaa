using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>Goods a customer brought back from a completed invoice, and the money refunded for them.</summary>
public class SalesReturn : BaseEntity<long>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public long Number { get; set; }

    public long SalesInvoiceId { get; set; }

    public int? CustomerId { get; set; }

    public int BranchId { get; set; }

    public int WarehouseId { get; set; }

    public RefundMethod RefundMethod { get; set; }

    public decimal Total { get; set; }

    /// <summary>Cost of the units put back on the shelf.</summary>
    public decimal TotalCost { get; set; }

    public int PointsReversed { get; set; }

    public string? Reason { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public string Code => FormatCode(Number);

    public SalesInvoice SalesInvoice { get; set; } = null!;

    public Customer? Customer { get; set; }

    public Branch Branch { get; set; } = null!;

    public Warehouse Warehouse { get; set; } = null!;

    public ICollection<SalesReturnItem> Items { get; set; } = [];

    public static string FormatCode(long number) => $"RET-{number:000000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Number.ToString(), Reason);
}

using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Warehouse : BaseEntity<int>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public int? BranchId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }

    /// <summary>How many units it holds when full; the capacity % on the screen is stock / capacity.</summary>
    public int? Capacity { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public string SearchText { get; set; } = string.Empty;

    public Branch? Branch { get; set; }

    public ICollection<StockItem> Stock { get; set; } = [];

    public string BuildSearchText() => ArabicSearch.Build(Name, Location);
}

using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Category : BaseEntity<int>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public int SortOrder { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = [];

    public string BuildSearchText() => ArabicSearch.Build(Name, Description);
}

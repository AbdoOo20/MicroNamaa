using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

/// <summary>A store of the merchant (Tripoli Central #01). Its POS sells from <see cref="DefaultWarehouseId" />.</summary>
public class Branch : BaseEntity<int>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public int? DefaultWarehouseId { get; set; }

    public bool IsActive { get; set; } = true;

    public Merchant Merchant { get; set; } = null!;

    public Warehouse? DefaultWarehouse { get; set; }

    public ICollection<Register> Registers { get; set; } = [];
}

using MicroNamaa.Domain.Common;

namespace MicroNamaa.Domain.Entities;

/// <summary>A cash register (POS device) in a branch (Reg 02).</summary>
public class Register : BaseEntity<int>, IMerchantOwned
{
    public int MerchantId { get; set; }

    public int BranchId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Branch Branch { get; set; } = null!;
}

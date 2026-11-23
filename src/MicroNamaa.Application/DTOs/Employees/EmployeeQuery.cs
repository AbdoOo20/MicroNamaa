using MicroNamaa.Application.Common;

namespace MicroNamaa.Application.DTOs.Employees;

public class EmployeeQuery : PagedQuery
{
    /// <summary>MerchantOwner, MerchantManager or Cashier.</summary>
    public string? Role { get; set; }

    public int? BranchId { get; set; }

    public bool? IsActive { get; set; }
}

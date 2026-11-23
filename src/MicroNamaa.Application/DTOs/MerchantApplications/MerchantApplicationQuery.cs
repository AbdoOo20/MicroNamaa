using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.MerchantApplications;

public class MerchantApplicationQuery : PagedQuery
{
    public MerchantApplicationStatus? Status { get; set; }
}

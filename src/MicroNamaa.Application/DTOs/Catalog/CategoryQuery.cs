using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Catalog;

public class CategoryQuery : PagedQuery
{
    public RecordStatus? Status { get; set; }
}

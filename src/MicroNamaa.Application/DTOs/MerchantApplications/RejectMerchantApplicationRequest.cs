using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.MerchantApplications;

public record RejectMerchantApplicationRequest
{
    [Required, MaxLength(500)]
    public string AdminNote { get; init; } = string.Empty;
}

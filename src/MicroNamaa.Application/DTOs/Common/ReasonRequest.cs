using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Common;

public record ReasonRequest
{
    [Required, MaxLength(500)]
    public string Reason { get; init; } = string.Empty;
}

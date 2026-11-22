using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Admin;

public record ResetUserPasswordRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}

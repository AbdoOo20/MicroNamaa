using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Auth;

public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}

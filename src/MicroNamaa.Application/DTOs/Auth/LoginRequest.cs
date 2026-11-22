using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Auth;

public record LoginRequest
{
    /// <summary>The email address or the mobile number.</summary>
    [Required, MaxLength(256)]
    public string Login { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

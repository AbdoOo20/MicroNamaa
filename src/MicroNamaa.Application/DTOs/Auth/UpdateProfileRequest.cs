using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Auth;

public record UpdateProfileRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress, MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }
}

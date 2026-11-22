using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Merchants;

public record RegisterRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

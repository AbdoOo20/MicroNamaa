using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Staff;

public record ResetStaffPasswordRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}

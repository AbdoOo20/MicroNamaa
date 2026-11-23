using System.ComponentModel.DataAnnotations;

namespace MicroNamaa.Application.DTOs.Employees;

public record ResetEmployeePasswordRequest
{
    [Required, MinLength(8), MaxLength(100)]
    public string NewPassword { get; init; } = string.Empty;
}

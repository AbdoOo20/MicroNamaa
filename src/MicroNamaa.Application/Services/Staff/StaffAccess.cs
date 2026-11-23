using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Staff;

/// <summary>What the signed-in admin may do: the owner may do everything, a staff member only what was granted.</summary>
public record StaffAccess(bool IsStaff, bool IsActive, IReadOnlyDictionary<AdminArea, AdminActions> Permissions)
{
    public static readonly StaffAccess Owner = new(false, true, new Dictionary<AdminArea, AdminActions>());

    public bool Can(AdminArea area, AdminActions action) => !IsStaff || (IsActive && Permissions.TryGetValue(area, out var granted) && granted.HasFlag(action));
}

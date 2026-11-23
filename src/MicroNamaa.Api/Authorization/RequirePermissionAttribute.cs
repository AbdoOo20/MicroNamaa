using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Authorization;

/// <summary>
/// What a staff member (sub-admin) needs to call this endpoint. The owner admin passes every check; merchant users
/// are not affected (their roles decide). On an action it replaces the controller's attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequirePermissionAttribute(AdminArea area, AdminActions action = AdminActions.View) : Attribute
{
    public AdminArea Area { get; } = area;

    public AdminActions Action { get; } = action;
}

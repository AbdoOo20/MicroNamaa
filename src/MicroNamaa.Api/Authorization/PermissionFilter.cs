using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Mvc.Filters;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Services.Staff;

namespace MicroNamaa.Api.Authorization;

/// <summary>Enforces <see cref="RequirePermissionAttribute"/> and <see cref="OwnerOnlyAttribute"/> for platform admins.</summary>
public sealed class PermissionFilter(IUserAppService userAppService, IStaffAccessService staffAccess) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;

        var ownerOnly = metadata.OfType<OwnerOnlyAttribute>().Any();

        var permission = metadata.OfType<RequirePermissionAttribute>().LastOrDefault();

        if ((!ownerOnly && permission is null) || !userAppService.IsAdmin)
        {
            return;
        }

        var access = await staffAccess.GetCurrentAsync(context.HttpContext.RequestAborted);

        if (!access.IsStaff)
        {
            return;
        }

        if (!access.IsActive)
        {
            context.Result = Refuse(Result.Unauthorized("This account is disabled."));

            return;
        }

        if (ownerOnly)
        {
            context.Result = Refuse(Result.Forbidden("Only the owner admin can do this."));

            return;
        }

        if (!access.Can(permission!.Area, permission.Action))
        {
            context.Result = Refuse(Result.Forbidden("You do not have permission to do this."));
        }
    }

    private static ObjectResult Refuse(Result result) => new(result) { StatusCode = result.StatusCode };
}

using System.Security.Claims;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Exceptions;

using MicroNamaa.Domain.Constants;

namespace MicroNamaa.Api.Services;

public class UserAppService(IHttpContextAccessor httpContextAccessor) : IUserAppService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public int? UserId => ReadInt(AppClaimTypes.UserId) ?? ReadInt(ClaimTypes.NameIdentifier);

    public int? MerchantId => ReadInt(AppClaimTypes.MerchantId);

    public int? BranchId => ReadInt(AppClaimTypes.BranchId);

    public string? Role => Principal?.FindFirst("role")?.Value ?? Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAdmin => HasRole(RoleNames.PlatformAdmin);

    public bool IsOwner => HasRole(RoleNames.MerchantOwner);

    public bool IsManager => IsOwner || HasRole(RoleNames.MerchantManager);

    public bool IsCashier => HasRole(RoleNames.Cashier) && !IsManager;

    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public int RequiredUserId => UserId ?? throw new UnauthorizedAccessException("The current user is not authenticated.");

    public int RequiredMerchantId => MerchantId ?? throw new ForbiddenException("This is for store accounts only.");

    private bool HasRole(string role) => Principal?.HasClaim("role", role) == true || Principal?.IsInRole(role) == true;

    private int? ReadInt(string claimType)
    {
        var value = Principal?.FindFirst(claimType)?.Value;

        return int.TryParse(value, out var result) ? result : null;
    }
}

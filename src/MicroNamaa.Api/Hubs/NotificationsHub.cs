using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.SignalR;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Domain.Constants;

namespace MicroNamaa.Api.Hubs;

/// <summary>
/// Real-time notifications. Connect to <see cref="Route" /> with <c>?access_token=</c> and listen to <see cref="ReceiveMethod" />.
/// Every connection joins its user group; owners and managers also join their merchant's group, platform admins the admins group.
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    public const string Route = "/hubs/notifications";

    public const string ReceiveMethod = "notification";

    public const string AdminsGroup = "admins";

    public static string UserGroup(int userId) => $"user:{userId}";

    public static string MerchantGroup(int merchantId) => $"merchant:{merchantId}";

    public override async Task OnConnectedAsync()
    {
        var principal = Context.User;

        if (int.TryParse(principal?.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        var isManager = principal?.HasClaim("role", RoleNames.MerchantOwner) == true || principal?.HasClaim("role", RoleNames.MerchantManager) == true;

        if (isManager && int.TryParse(principal?.FindFirst(AppClaimTypes.MerchantId)?.Value, out var merchantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, MerchantGroup(merchantId));
        }

        if (principal?.HasClaim("role", RoleNames.PlatformAdmin) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
        }

        await base.OnConnectedAsync();
    }
}

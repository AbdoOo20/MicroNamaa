using Microsoft.AspNetCore.SignalR;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Hubs;

public class SignalRNotificationPublisher(IHubContext<NotificationsHub> hub, ILogger<SignalRNotificationPublisher> logger) : INotificationPublisher
{
    public async Task PublishAsync(IReadOnlyCollection<NotificationPush> pushes, CancellationToken cancellationToken = default)
    {
        foreach (var push in pushes)
        {
            var group = push.Audience switch
            {
                NotificationAudience.User when push.UserId is { } userId => NotificationsHub.UserGroup(userId),

                NotificationAudience.Merchant when push.MerchantId is { } merchantId => NotificationsHub.MerchantGroup(merchantId),

                NotificationAudience.Admins => NotificationsHub.AdminsGroup,

                _ => null
            };

            if (group is null)
            {
                continue;
            }

            try
            {
                await hub.Clients.Group(group).SendAsync(NotificationsHub.ReceiveMethod, push.Notification, cancellationToken);
            }

            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not push notification {NotificationId} over SignalR.", push.Notification.Id);
            }
        }
    }
}

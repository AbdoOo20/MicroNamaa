using MicroNamaa.Application.DTOs.Notifications;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Abstractions;

public record NotificationPush(NotificationAudience Audience, int? UserId, int? MerchantId, NotificationDto Notification);

public interface INotificationPublisher
{
    Task PublishAsync(IReadOnlyCollection<NotificationPush> pushes, CancellationToken cancellationToken = default);
}

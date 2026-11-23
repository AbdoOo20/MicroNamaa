using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Notifications;

namespace MicroNamaa.Application.Services.Notifications;

public interface INotificationService
{
    Task<Result<PagedResult<NotificationDto>>> GetAllAsync(NotificationQuery query, CancellationToken cancellationToken = default);

    Task<Result<int>> GetUnreadCountAsync(CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(long id, CancellationToken cancellationToken = default);

    Task<Result> MarkAllReadAsync(CancellationToken cancellationToken = default);
}

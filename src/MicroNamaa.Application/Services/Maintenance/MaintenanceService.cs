using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Maintenance;

/// <summary>
/// Runs every few minutes: reminds merchants (and the platform admins) a few days before a subscription ends,
/// once per subscription period.
/// </summary>
public class MaintenanceService(IUnitOfWork unitOfWork, IOptions<PlatformSettings> platformOptions) : IMaintenanceService
{
    private const int BatchSize = 200;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await RemindSubscriptionsAsync(cancellationToken);
    }

    private async Task RemindSubscriptionsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var limit = now.AddDays(platformOptions.Value.SubscriptionReminderDays);

        var merchants = await unitOfWork.Repository<Merchant>().Query(asNoTracking: false)
            .Where(merchant => merchant.Status == MerchantStatus.Active && merchant.SubscriptionEndsAt != null && merchant.SubscriptionEndsAt > now && merchant.SubscriptionEndsAt <= limit)
            .Where(merchant => merchant.SubscriptionReminderSentAt == null || merchant.SubscriptionReminderSentAt < merchant.SubscriptionEndsAt!.Value.AddDays(-platformOptions.Value.SubscriptionReminderDays - 1))
            .OrderBy(merchant => merchant.Id)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var merchant in merchants)
        {
            var endsOn = LibyaTime.ToLocal(merchant.SubscriptionEndsAt!.Value).ToString("yyyy-MM-dd");

            merchant.SubscriptionReminderSentAt = now;

            await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(merchant.Id, NotificationTypes.SubscriptionEnding, "اشتراكك قرّب يخلص", $"الاشتراك ينتهي يوم {endsOn}. تواصل معانا للتجديد عشان الشغل ما يقفش.", RelatedTypes.Merchant, merchant.Id), cancellationToken);

            await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForAdmins(NotificationTypes.SubscriptionEnding, $"اشتراك {merchant.Name} ينتهي {endsOn}", null, RelatedTypes.Merchant, merchant.Id, merchant.Id), cancellationToken);
        }

        if (merchants.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}

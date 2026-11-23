using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Caching.Memory;

using Microsoft.Extensions.DependencyInjection;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Domain.Enums;

using MicroNamaa.Infrastructure.Persistence;

namespace MicroNamaa.Infrastructure.Services.Auth;

public class MerchantAccessService(IMemoryCache cache, IServiceScopeFactory scopeFactory) : IMerchantAccessService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public async Task<bool> IsAllowedAsync(int merchantId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey(merchantId), out bool cached))
        {
            return cached;
        }

        using var scope = scopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var merchant = await context.Merchants
            .AsNoTracking()
            .Where(item => item.Id == merchantId)
            .Select(item => new { item.Status, item.SubscriptionEndsAt })
            .FirstOrDefaultAsync(cancellationToken);

        var allowed = merchant is not null && merchant.Status == MerchantStatus.Active && (merchant.SubscriptionEndsAt is null || merchant.SubscriptionEndsAt > DateTime.UtcNow);

        cache.Set(CacheKey(merchantId), allowed, CacheDuration);

        return allowed;
    }

    public void Forget(int merchantId)
    {
        cache.Remove(CacheKey(merchantId));
    }

    private static string CacheKey(int merchantId) => $"merchant-allowed:{merchantId}";
}

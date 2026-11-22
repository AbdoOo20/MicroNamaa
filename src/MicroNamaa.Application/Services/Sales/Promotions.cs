using System.Linq.Expressions;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Sales;

public static class Promotions
{
    /// <summary>Turned on and inside its dates.</summary>
    public static Expression<Func<Promotion, bool>> IsRunning(DateTime now) => promotion => promotion.IsActive && (promotion.StartsAt == null || promotion.StartsAt <= now) && (promotion.EndsAt == null || promotion.EndsAt > now);
}

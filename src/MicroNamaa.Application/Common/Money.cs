namespace MicroNamaa.Application.Common;

/// <summary>LYD amounts. The dinar has 1000 dirhams, so amounts are kept to three decimals.</summary>
public static class Money
{
    public const string Currency = "LYD";

    public static decimal Round(decimal amount) => Math.Round(amount, 3, MidpointRounding.AwayFromZero);

    public static decimal Percent(decimal amount, decimal percent) => Round(amount * percent / 100m);

    /// <summary>The change from <paramref name="previous" /> to <paramref name="current" /> in percent, or null when there was nothing before.</summary>
    public static decimal? Growth(decimal current, decimal previous) => previous == 0 ? null : Math.Round((current - previous) / previous * 100m, 1);
}

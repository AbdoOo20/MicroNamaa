namespace MicroNamaa.Application.Abstractions;

/// <summary>
/// Tells whether a merchant's users may use the system right now (not suspended, subscription not ended).
/// The answer is cached for a few seconds, and <see cref="Forget"/> clears it when an admin changes the merchant.
/// </summary>
public interface IMerchantAccessService
{
    const string BlockedMessage = "This account is suspended or its subscription has ended. Contact support.";

    Task<bool> IsAllowedAsync(int merchantId, CancellationToken cancellationToken = default);

    void Forget(int merchantId);
}

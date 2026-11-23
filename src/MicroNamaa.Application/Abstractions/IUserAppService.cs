namespace MicroNamaa.Application.Abstractions;

public interface IUserAppService
{
    bool IsAuthenticated { get; }

    int? UserId { get; }

    /// <summary>The merchant of the signed-in user; empty for platform admins and anonymous calls.</summary>
    int? MerchantId { get; }

    /// <summary>The branch a cashier is tied to.</summary>
    int? BranchId { get; }

    string? Role { get; }

    bool IsAdmin { get; }

    bool IsOwner { get; }

    /// <summary>A merchant owner or manager.</summary>
    bool IsManager { get; }

    bool IsCashier { get; }

    string? IpAddress { get; }

    int RequiredUserId { get; }

    int RequiredMerchantId { get; }
}

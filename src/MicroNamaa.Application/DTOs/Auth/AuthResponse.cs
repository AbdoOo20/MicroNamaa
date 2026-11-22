namespace MicroNamaa.Application.DTOs.Auth;

public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, int UserId, int? MerchantId, int? BranchId, string FullName, string Role);

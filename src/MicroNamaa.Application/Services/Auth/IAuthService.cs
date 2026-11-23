using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Auth;

namespace MicroNamaa.Application.Services.Auth;

/// <summary>Sign-in and the signed-in user's own account. Nobody signs up: the admin creates merchants and merchants create their employees.</summary>
public interface IAuthService
{
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<Result<CurrentUserDto>> SetAvatarAsync(FileUpload? file, CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

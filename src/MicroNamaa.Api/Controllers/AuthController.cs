using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Auth;

using MicroNamaa.Application.Services.Auth;

namespace MicroNamaa.Api.Controllers;

public class AuthController(IAuthService authService) : ApiControllerBase
{
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("login")]
    public async Task<ActionResult<Result<AuthResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Respond(await authService.LoginAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<Result<CurrentUserDto>>> Me(CancellationToken cancellationToken)
    {
        return Respond(await authService.GetCurrentUserAsync(cancellationToken));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<Result<CurrentUserDto>>> UpdateProfile(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        return Respond(await authService.UpdateProfileAsync(request, cancellationToken));
    }

    /// <summary>multipart/form-data with the image in "file" (JPG, PNG or WEBP).</summary>
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [HttpPost("profile/avatar")]
    public async Task<ActionResult<Result<CurrentUserDto>>> SetAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await authService.SetAvatarAsync(Upload(file), cancellationToken));
    }

    [Authorize]
    [HttpDelete("profile/avatar")]
    public async Task<ActionResult<Result<CurrentUserDto>>> RemoveAvatar(CancellationToken cancellationToken)
    {
        return Respond(await authService.SetAvatarAsync(null, cancellationToken));
    }

    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [HttpPost("change-password")]
    public async Task<ActionResult<Result>> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        return Respond(await authService.ChangePasswordAsync(request, cancellationToken));
    }
}

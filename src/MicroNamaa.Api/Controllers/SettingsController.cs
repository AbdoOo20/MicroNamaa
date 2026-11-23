using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.Services.Merchants;

namespace MicroNamaa.Api.Controllers;

/// <summary>The store's profile and settings. Everyone in the store reads them; only the owner changes them.</summary>
[Authorize(Roles = AppRoles.MerchantUsers)]
public class SettingsController(IMerchantSettingsService settingsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<MerchantSettingsDto>>> Get(CancellationToken cancellationToken)
    {
        return Respond(await settingsService.GetAsync(cancellationToken));
    }

    [HttpPut]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<ActionResult<Result<MerchantSettingsDto>>> Update(UpdateMerchantSettingsRequest request, CancellationToken cancellationToken)
    {
        return Respond(await settingsService.UpdateAsync(request, cancellationToken));
    }

    /// <summary>multipart/form-data with the logo in "file".</summary>
    [HttpPost("logo")]
    [Authorize(Roles = AppRoles.Owner)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<MerchantSettingsDto>>> SetLogo(IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await settingsService.SetLogoAsync(Upload(file), cancellationToken));
    }

    [HttpDelete("logo")]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<ActionResult<Result<MerchantSettingsDto>>> RemoveLogo(CancellationToken cancellationToken)
    {
        return Respond(await settingsService.RemoveLogoAsync(cancellationToken));
    }
}

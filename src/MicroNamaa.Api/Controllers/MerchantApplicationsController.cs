using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.MerchantApplications;

using MicroNamaa.Application.Services.MerchantApplications;

namespace MicroNamaa.Api.Controllers;

/// <summary>The "subscribe" form of the landing page.</summary>
[AllowAnonymous]
[Route("api/merchant-applications")]
public class MerchantApplicationsController(IMerchantApplicationService applicationService) : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<Result>> Submit(SubmitMerchantApplicationRequest request, CancellationToken cancellationToken)
    {
        return Respond(await applicationService.SubmitAsync(request, cancellationToken));
    }
}

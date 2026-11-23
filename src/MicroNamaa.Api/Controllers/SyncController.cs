using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Sync;

using MicroNamaa.Application.Services.Sync;

namespace MicroNamaa.Api.Controllers;

/// <summary>Offline work: ping to know the server is reachable, pull data for the device, push what was done offline.</summary>
[Authorize(Roles = AppRoles.MerchantUsers)]
public class SyncController(ISyncService syncService) : ApiControllerBase
{
    /// <summary>Cheap check for the Sync button: reachable server = online.</summary>
    [AllowAnonymous]
    [HttpGet("ping")]
    public ActionResult<Result<DateTime>> Ping()
    {
        return Respond(Result<DateTime>.Ok(DateTime.UtcNow));
    }

    [HttpGet("operations")]
    public ActionResult<Result<IReadOnlyList<SyncOperationTypeDto>>> GetOperations()
    {
        return Respond(syncService.GetOperationTypes());
    }

    /// <summary>?since= the serverTime of the last pull (empty = everything).</summary>
    [HttpGet("pull")]
    public async Task<ActionResult<Result<SyncPullDto>>> Pull([FromQuery] DateTime? since, CancellationToken cancellationToken)
    {
        return Respond(await syncService.PullAsync(since, cancellationToken));
    }

    [HttpPost("push")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<Result<SyncPushResultDto>>> Push(SyncPushRequest request, CancellationToken cancellationToken)
    {
        return Respond(await syncService.PushAsync(request, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Support;

using MicroNamaa.Application.Services.Support;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Api.Controllers;

/// <summary>The help center: store owners and managers open tickets; platform admins answer them.</summary>
[Authorize(Roles = AppRoles.Support)]
[RequirePermission(AdminArea.Tickets)]
public class TicketsController(ITicketService ticketService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<TicketDto>>>> GetAll([FromQuery] TicketQuery query, CancellationToken cancellationToken)
    {
        return Respond(await ticketService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<Result<TicketDetailDto>>> Get(long id, CancellationToken cancellationToken)
    {
        return Respond(await ticketService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<TicketDetailDto>>> Create(CreateTicketRequest request, CancellationToken cancellationToken)
    {
        return Respond(await ticketService.CreateAsync(request, cancellationToken));
    }

    [HttpPost("{id:long}/messages")]
    [RequirePermission(AdminArea.Tickets, AdminActions.Update)]
    public async Task<ActionResult<Result<TicketDetailDto>>> AddMessage(long id, TicketMessageRequest request, CancellationToken cancellationToken)
    {
        return Respond(await ticketService.AddMessageAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:long}/close")]
    [RequirePermission(AdminArea.Tickets, AdminActions.Update)]
    public async Task<ActionResult<Result<TicketDetailDto>>> Close(long id, CancellationToken cancellationToken)
    {
        return Respond(await ticketService.CloseAsync(id, cancellationToken));
    }
}

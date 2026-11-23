using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Staff;

using MicroNamaa.Application.Services.Staff;

namespace MicroNamaa.Api.Controllers;

/// <summary>Platform sub-admin accounts and their permissions; only the owner admin manages them.</summary>
[Authorize(Roles = AppRoles.Admin)]
[OwnerOnly]
[Route("api/admin/staff")]
public class AdminStaffController(IStaffService staffService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<StaffMemberDto>>>> GetAll(CancellationToken cancellationToken)
    {
        return Respond(await staffService.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<StaffMemberDto>>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        return Respond(await staffService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<StaffMemberDto>>> Update(int id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        return Respond(await staffService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/password")]
    public async Task<ActionResult<Result>> ResetPassword(int id, ResetStaffPasswordRequest request, CancellationToken cancellationToken)
    {
        return Respond(await staffService.ResetPasswordAsync(id, request, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.Services.Merchants;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.MerchantUsers)]
public class BranchesController(IBranchService branchService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<IReadOnlyList<BranchDto>>>> GetAll(CancellationToken cancellationToken)
    {
        return Respond(await branchService.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<BranchDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await branchService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<ActionResult<Result<BranchDto>>> Create(BranchRequest request, CancellationToken cancellationToken)
    {
        return Respond(await branchService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<BranchDto>>> Update(int id, BranchRequest request, CancellationToken cancellationToken)
    {
        return Respond(await branchService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Owner)]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await branchService.DeleteAsync(id, cancellationToken));
    }

    [HttpPost("{id:int}/registers")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<RegisterDto>>> AddRegister(int id, RegisterRequest request, CancellationToken cancellationToken)
    {
        return Respond(await branchService.AddRegisterAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:int}/registers/{registerId:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<RegisterDto>>> UpdateRegister(int id, int registerId, RegisterRequest request, CancellationToken cancellationToken)
    {
        return Respond(await branchService.UpdateRegisterAsync(id, registerId, request, cancellationToken));
    }

    [HttpDelete("{id:int}/registers/{registerId:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result>> DeleteRegister(int id, int registerId, CancellationToken cancellationToken)
    {
        return Respond(await branchService.DeleteRegisterAsync(id, registerId, cancellationToken));
    }
}

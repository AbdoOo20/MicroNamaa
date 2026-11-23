using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.RateLimiting;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Employees;

using MicroNamaa.Application.Services.Employees;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.Managers)]
public class EmployeesController(IEmployeeService employeeService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<EmployeeDto>>>> GetAll([FromQuery] EmployeeQuery query, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<Result<EmployeeSummaryDto>>> GetSummary(CancellationToken cancellationToken)
    {
        return Respond(await employeeService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] EmployeeQuery query, CancellationToken cancellationToken)
    {
        return Download(await employeeService.ExportAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<EmployeeDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Result<EmployeeDto>>> Create(CreateEmployeeRequest request, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Result<EmployeeDto>>> Update(int id, UpdateEmployeeRequest request, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:int}/password")]
    public async Task<ActionResult<Result>> ResetPassword(int id, ResetEmployeePasswordRequest request, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.ResetPasswordAsync(id, request, cancellationToken));
    }

    /// <summary>multipart/form-data with the photo in "file".</summary>
    [HttpPost("{id:int}/avatar")]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<Result<EmployeeDto>>> SetAvatar(int id, IFormFile file, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.SetAvatarAsync(id, Upload(file), cancellationToken));
    }

    [HttpDelete("{id:int}/avatar")]
    public async Task<ActionResult<Result<EmployeeDto>>> RemoveAvatar(int id, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.SetAvatarAsync(id, null, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await employeeService.DeleteAsync(id, cancellationToken));
    }
}

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Api.Authorization;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.Services.Merchants;

namespace MicroNamaa.Api.Controllers;

[Authorize(Roles = AppRoles.MerchantUsers)]
public class PromotionsController(IPromotionService promotionService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Result<PagedResult<PromotionDto>>>> GetAll([FromQuery] PromotionQuery query, CancellationToken cancellationToken)
    {
        return Respond(await promotionService.GetAllAsync(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Result<PromotionDto>>> Get(int id, CancellationToken cancellationToken)
    {
        return Respond(await promotionService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<PromotionDto>>> Create(PromotionRequest request, CancellationToken cancellationToken)
    {
        return Respond(await promotionService.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result<PromotionDto>>> Update(int id, PromotionRequest request, CancellationToken cancellationToken)
    {
        return Respond(await promotionService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<Result>> Delete(int id, CancellationToken cancellationToken)
    {
        return Respond(await promotionService.DeleteAsync(id, cancellationToken));
    }
}

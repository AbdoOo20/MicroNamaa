using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Lookups;

using MicroNamaa.Application.Services.Lookups;

namespace MicroNamaa.Api.Controllers;

[AllowAnonymous]
public class LookupsController(ILookupService lookupService) : ApiControllerBase
{
    [HttpGet("enums")]
    public ActionResult<Result<IReadOnlyDictionary<string, IReadOnlyList<EnumItemDto>>>> GetEnums()
    {
        return Respond(lookupService.GetEnums());
    }
}

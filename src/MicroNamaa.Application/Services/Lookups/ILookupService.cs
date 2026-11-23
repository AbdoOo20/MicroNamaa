using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Lookups;

namespace MicroNamaa.Application.Services.Lookups;

public interface ILookupService
{
    /// <summary>The names and values of every enum the API uses, keyed by enum name.</summary>
    Result<IReadOnlyDictionary<string, IReadOnlyList<EnumItemDto>>> GetEnums();
}

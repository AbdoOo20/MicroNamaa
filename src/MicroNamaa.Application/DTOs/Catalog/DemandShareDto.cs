namespace MicroNamaa.Application.DTOs.Catalog;

/// <summary>The product's units against all units of its category in one month.</summary>
public record DemandShareDto(int Year, int Month, int ProductUnits, int CategoryUnits, decimal? SharePercent);

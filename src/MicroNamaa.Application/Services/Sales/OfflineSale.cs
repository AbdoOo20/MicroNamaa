namespace MicroNamaa.Application.Services.Sales;

/// <summary>A sale made on a device with no internet: when it happened (UTC) and the device's key for it.</summary>
public record OfflineSale(DateTime At, string? ClientKey);

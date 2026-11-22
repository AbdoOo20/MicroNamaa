namespace MicroNamaa.Application.DTOs.Sales;

public record PosLoyaltyDto(bool Enabled, decimal PointsPerCurrencyUnit, decimal PointValue, int MinPointsToRedeem);

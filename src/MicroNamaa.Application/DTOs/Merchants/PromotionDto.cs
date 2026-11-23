using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Merchants;

public record PromotionDto(int Id, string Name, DiscountType Type, decimal Value, PromotionScope Scope, int? ProductId, string? ProductName, int? CategoryId, string? CategoryName, decimal? MinOrderAmount, DateTime? StartsAt, DateTime? EndsAt, bool IsActive, bool IsRunning);

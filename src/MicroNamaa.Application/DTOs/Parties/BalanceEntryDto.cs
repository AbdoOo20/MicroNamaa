using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

/// <summary>A statement line. For customers a positive amount adds to what they owe; for suppliers, to what the merchant owes.</summary>
public record BalanceEntryDto(long Id, BalanceEntryType Type, decimal Amount, decimal BalanceAfter, PaymentMethod? PaymentMethod, string? Reference, long? DocumentId, string? Note, string? CreatedByName, DateTime CreatedAt);

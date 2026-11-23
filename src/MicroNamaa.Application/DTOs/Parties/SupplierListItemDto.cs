using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record SupplierListItemDto(int Id, string Code, string Name, string? ContactPerson, string? Phone, string? Email, string? LogoUrl, decimal Balance, int PurchasesCount, DateTime? LastPurchaseAt, RecordStatus Status, DateTime CreatedAt);

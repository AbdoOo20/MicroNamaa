using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Parties;

public record SupplierDetailDto(
    int Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? City,
    string? Address,
    string? TaxNumber,
    string? LogoUrl,
    decimal Balance,
    RecordStatus Status,
    string? Notes,
    int PurchasesCount,
    decimal TotalPurchased,
    decimal TotalPaid,
    DateTime? LastPurchaseAt,
    DateTime CreatedAt);

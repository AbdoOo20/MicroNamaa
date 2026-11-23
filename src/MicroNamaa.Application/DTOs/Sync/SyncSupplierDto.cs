using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.Sync;

public record SyncSupplierDto(int Id, string Code, string Name, string? ContactPerson, string? Phone, string? Email, string? City, string? Address, string? TaxNumber, string? LogoUrl, decimal Balance, RecordStatus Status, string? Notes, DateTime UpdatedAt);

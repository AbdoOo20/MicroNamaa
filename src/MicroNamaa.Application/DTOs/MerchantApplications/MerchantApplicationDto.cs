using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.DTOs.MerchantApplications;

public record MerchantApplicationDto(int Id, string BusinessName, string OwnerName, string Phone, string? Email, string? City, string? BusinessType, int? BranchesCount, string? Notes, MerchantApplicationStatus Status, string? AdminNote, int? MerchantId, DateTime? ReviewedAt, DateTime CreatedAt);

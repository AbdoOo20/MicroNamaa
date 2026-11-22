namespace MicroNamaa.Application.DTOs.Sales;

public record InvoiceCustomerDto(int Id, string Code, string Name, string? Phone, string? AvatarUrl, decimal Balance, int LoyaltyPoints);

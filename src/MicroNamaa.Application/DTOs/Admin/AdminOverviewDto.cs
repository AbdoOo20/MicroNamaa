namespace MicroNamaa.Application.DTOs.Admin;

public record AdminOverviewDto(int Merchants, int ActiveMerchants, int SuspendedMerchants, int ExpiringSoon, int ExpiredSubscriptions, int PendingApplications, int OpenTickets, decimal PlatformSalesLast30Days, int InvoicesLast30Days, IReadOnlyList<TopMerchantDto> TopMerchants);

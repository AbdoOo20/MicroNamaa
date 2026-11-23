using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Support;

namespace MicroNamaa.Application.Services.Support;

/// <summary>
/// The help center: a merchant's owner or manager opens a ticket and the platform's support team answers it.
/// Every user of the merchant who may see tickets sees the merchant's tickets.
/// </summary>
public interface ITicketService
{
    Task<Result<PagedResult<TicketDto>>> GetAllAsync(TicketQuery query, CancellationToken cancellationToken = default);

    Task<Result<TicketDetailDto>> GetAsync(long id, CancellationToken cancellationToken = default);

    Task<Result<TicketDetailDto>> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);

    Task<Result<TicketDetailDto>> AddMessageAsync(long id, TicketMessageRequest request, CancellationToken cancellationToken = default);

    Task<Result<TicketDetailDto>> CloseAsync(long id, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Support;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Support;

public class TicketService(IUnitOfWork unitOfWork, IUserAppService userAppService, IOptions<PlatformSettings> platformOptions) : ITicketService
{
    private readonly string supportName = platformOptions.Value.SupportName;

    public async Task<Result<PagedResult<TicketDto>>> GetAllAsync(TicketQuery query, CancellationToken cancellationToken = default)
    {
        var tickets = Visible();

        if (query.Status is { } status)
        {
            tickets = tickets.Where(ticket => ticket.Status == status);
        }

        if (userAppService.IsAdmin && query.MerchantId is { } merchantId)
        {
            tickets = tickets.Where(ticket => ticket.MerchantId == merchantId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            tickets = tickets.Where(ticket => ticket.Subject.Contains(search));
        }

        var page = await tickets
            .OrderBy(ticket => ticket.Status == TicketStatus.Closed ? 1 : 0)
            .ThenByDescending(ticket => ticket.LastMessageAt)
            .Select(ticket => new TicketDto(ticket.Id, ticket.Subject, ticket.Category, ticket.Status, ticket.MerchantId, ticket.Merchant == null ? null : ticket.Merchant.Name, ticket.OpenedBy.FullName, ticket.LastMessageAt, ticket.CreatedAt))
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<TicketDto>>.Ok(page);
    }

    public async Task<Result<TicketDetailDto>> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        var detail = await LoadAsync(id, cancellationToken);

        return detail is null ? Result<TicketDetailDto>.NotFound("Ticket was not found.") : Result<TicketDetailDto>.Ok(detail);
    }

    public async Task<Result<TicketDetailDto>> CreateAsync(CreateTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (userAppService.IsAdmin)
        {
            return Result<TicketDetailDto>.Forbidden("Admins answer tickets; they do not open them.");
        }

        var userId = userAppService.RequiredUserId;

        var merchantId = userAppService.RequiredMerchantId;

        var ticket = new Ticket
        {
            OpenedById = userId,

            MerchantId = merchantId,

            Category = request.Category,

            Subject = request.Subject.Trim(),

            LastMessageAt = DateTime.UtcNow,

            Messages = [new TicketMessage { SenderId = userId, Body = request.Body.Trim() }]
        };

        await unitOfWork.Repository<Ticket>().AddAsync(ticket, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var merchantName = await unitOfWork.Repository<Merchant>().Query().Where(merchant => merchant.Id == merchantId).Select(merchant => merchant.Name).FirstOrDefaultAsync(cancellationToken);

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForAdmins(NotificationTypes.TicketCreated, $"تذكرة جديدة من {merchantName}: {ticket.Subject}", Shorten(request.Body), RelatedTypes.Ticket, ticket.Id, merchantId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TicketDetailDto>.Created((await LoadAsync(ticket.Id, cancellationToken))!);
    }

    public async Task<Result<TicketDetailDto>> AddMessageAsync(long id, TicketMessageRequest request, CancellationToken cancellationToken = default)
    {
        var ticket = await Visible(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return Result<TicketDetailDto>.NotFound("Ticket was not found.");
        }

        if (ticket.Status == TicketStatus.Closed)
        {
            return Result<TicketDetailDto>.Conflict("This ticket is closed.");
        }

        var isAdmin = userAppService.IsAdmin;

        var body = request.Body.Trim();

        await unitOfWork.Repository<TicketMessage>().AddAsync(new TicketMessage { TicketId = ticket.Id, SenderId = userAppService.RequiredUserId, IsFromAdmin = isAdmin, Body = body }, cancellationToken);

        ticket.LastMessageAt = DateTime.UtcNow;

        ticket.Status = isAdmin ? TicketStatus.Answered : TicketStatus.Open;

        var notification = isAdmin
            ? NotificationFactory.ForUser(ticket.OpenedById, NotificationTypes.TicketReply, $"رد من {supportName}: {ticket.Subject}", Shorten(body), RelatedTypes.Ticket, ticket.Id)
            : NotificationFactory.ForAdmins(NotificationTypes.TicketReply, $"رد جديد: {ticket.Subject}", Shorten(body), RelatedTypes.Ticket, ticket.Id, ticket.MerchantId);

        await unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TicketDetailDto>.Ok((await LoadAsync(ticket.Id, cancellationToken))!);
    }

    public async Task<Result<TicketDetailDto>> CloseAsync(long id, CancellationToken cancellationToken = default)
    {
        var ticket = await Visible(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (ticket is null)
        {
            return Result<TicketDetailDto>.NotFound("Ticket was not found.");
        }

        ticket.Status = TicketStatus.Closed;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<TicketDetailDto>.Ok((await LoadAsync(ticket.Id, cancellationToken))!);
    }

    private IQueryable<Ticket> Visible(bool asNoTracking = true)
    {
        var tickets = unitOfWork.Repository<Ticket>().Query(asNoTracking);

        if (userAppService.IsAdmin)
        {
            return tickets;
        }

        var merchantId = userAppService.RequiredMerchantId;

        return tickets.Where(ticket => ticket.MerchantId == merchantId);
    }

    private async Task<TicketDetailDto?> LoadAsync(long id, CancellationToken cancellationToken)
    {
        var isAdmin = userAppService.IsAdmin;

        var ticket = await Visible()
            .Where(item => item.Id == id)
            .Select(item => new TicketDto(item.Id, item.Subject, item.Category, item.Status, item.MerchantId, item.Merchant == null ? null : item.Merchant.Name, item.OpenedBy.FullName, item.LastMessageAt, item.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var supportName = this.supportName;

        var messages = await unitOfWork.Repository<TicketMessage>().Query()
            .Where(message => message.TicketId == id)
            .OrderBy(message => message.Id)
            .Select(message => new TicketMessageDto(message.Id, message.Body, message.IsFromAdmin, message.IsFromAdmin && !isAdmin ? supportName : message.Sender.FullName, message.CreatedAt))
            .ToListAsync(cancellationToken);

        return new TicketDetailDto(ticket, messages);
    }

    private static string Shorten(string text) => text.Length <= 200 ? text : text[..200] + "…";
}

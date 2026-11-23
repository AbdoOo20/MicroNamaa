using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Admin;

public class AdminMerchantService(IUnitOfWork unitOfWork, IMerchantProvisioningService provisioning, IMerchantAccessService merchantAccess) : IAdminMerchantService
{
    private const string NotFound = "Merchant was not found.";

    public async Task<Result<PagedResult<AdminMerchantListItemDto>>> GetAllAsync(AdminMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var merchants = unitOfWork.Repository<Merchant>().Query();

        if (query.Status is { } status)
        {
            merchants = merchants.Where(merchant => merchant.Status == status);
        }

        if (query.ExpiringWithinDays is { } days)
        {
            var limit = DateTime.UtcNow.AddDays(days);

            merchants = merchants.Where(merchant => merchant.SubscriptionEndsAt != null && merchant.SubscriptionEndsAt <= limit);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            merchants = merchants.WhereMatchesSearch(query.Search);
        }

        var now = DateTime.UtcNow;

        var since = now.AddDays(-30);

        var invoices = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.Status == SalesInvoiceStatus.Completed);

        var branches = unitOfWork.Repository<Branch>().Query();

        var users = unitOfWork.Repository<User>().Query();

        var page = await merchants
            .OrderBy(merchant => merchant.Status)
            .ThenByDescending(merchant => merchant.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(merchant => new AdminMerchantListItemDto(
                    merchant.Id,
                    Merchant.FormatCode(merchant.Number),
                    merchant.Name,
                    merchant.OwnerName,
                    merchant.Phone,
                    merchant.City,
                    merchant.LogoPath == null ? null : FileUrls.MerchantLogo(merchant.Id, merchant.LogoPath, merchant.UpdatedAt),
                    merchant.Status,
                    merchant.SubscriptionEndsAt,
                    merchant.SubscriptionEndsAt == null || merchant.SubscriptionEndsAt > now,
                    branches.Count(branch => branch.MerchantId == merchant.Id),
                    users.Count(user => user.MerchantId == merchant.Id),
                    invoices.Where(invoice => invoice.MerchantId == merchant.Id && invoice.CompletedAt >= since).Sum(invoice => (decimal?)(invoice.Total - invoice.ReturnedAmount)) ?? 0,
                    invoices.Where(invoice => invoice.MerchantId == merchant.Id).Max(invoice => invoice.CompletedAt),
                    merchant.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<AdminMerchantListItemDto>>.Ok(page);
    }

    public async Task<Result<AdminMerchantDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var merchant = await LoadAsync(id, cancellationToken);

        return merchant is null ? Result<AdminMerchantDetailDto>.NotFound(NotFound) : Result<AdminMerchantDetailDto>.Ok(merchant);
    }

    public async Task<Result<AdminMerchantDetailDto>> CreateAsync(CreateMerchantRequest request, CancellationToken cancellationToken = default)
    {
        var created = await provisioning.CreateAsync(request, cancellationToken);

        if (!created.Succeeded)
        {
            return Result<AdminMerchantDetailDto>.From(created);
        }

        return Result<AdminMerchantDetailDto>.Created((await LoadAsync(created.Data, cancellationToken))!, "Merchant created.");
    }

    public async Task<Result<AdminMerchantDetailDto>> UpdateAsync(int id, UpdateMerchantRequest request, CancellationToken cancellationToken = default)
    {
        var merchant = await Tracked(id, cancellationToken);

        if (merchant is null)
        {
            return Result<AdminMerchantDetailDto>.NotFound(NotFound);
        }

        var phone = LibyanPhone.Normalize(request.Phone);

        if (phone is null)
        {
            return Result<AdminMerchantDetailDto>.BadRequest(LibyanPhone.InvalidMessage);
        }

        merchant.Name = request.Name.Trim();

        merchant.OwnerName = request.OwnerName.Trim();

        merchant.Phone = phone;

        merchant.Email = Clean(request.Email);

        merchant.City = Clean(request.City);

        merchant.Address = Clean(request.Address);

        merchant.TaxNumber = Clean(request.TaxNumber);

        merchant.MaxBranches = request.MaxBranches;

        merchant.MaxUsers = request.MaxUsers;

        merchant.Notes = Clean(request.Notes);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AdminMerchantDetailDto>.Ok((await LoadAsync(id, cancellationToken))!, "Merchant updated.");
    }

    public async Task<Result<AdminMerchantDetailDto>> UpdateStatusAsync(int id, UpdateMerchantStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.Status))
        {
            return Result<AdminMerchantDetailDto>.BadRequest("Status must be Active (1) or Suspended (2).");
        }

        var merchant = await Tracked(id, cancellationToken);

        if (merchant is null)
        {
            return Result<AdminMerchantDetailDto>.NotFound(NotFound);
        }

        if (merchant.Status != request.Status)
        {
            merchant.Status = request.Status;

            var (title, body) = request.Status == MerchantStatus.Active
                ? ("حسابك اتفعّل تاني", "تقدر ترجع تشتغل على النظام عادي.")
                : ("حسابك اتوقف مؤقتاً", request.Note ?? "تواصل مع الدعم لمعرفة السبب.");

            await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(id, NotificationTypes.MerchantStatusChanged, title, body, RelatedTypes.Merchant, id), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            merchantAccess.Forget(id);
        }

        return Result<AdminMerchantDetailDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<AdminMerchantDetailDto>> UpdateSubscriptionAsync(int id, UpdateSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var merchant = await Tracked(id, cancellationToken);

        if (merchant is null)
        {
            return Result<AdminMerchantDetailDto>.NotFound(NotFound);
        }

        var now = DateTime.UtcNow;

        if (request.Unlimited)
        {
            merchant.SubscriptionEndsAt = null;
        }

        else if (request.EndsOn is { } endsOn)
        {
            merchant.SubscriptionEndsAt = EndOfDay(endsOn);
        }

        else if (request.ExtendDays is { } days)
        {
            var from = merchant.SubscriptionEndsAt is { } current && current > now ? current : EndOfDay(DateOnly.FromDateTime(LibyaTime.Now));

            merchant.SubscriptionEndsAt = from.AddDays(days);
        }

        else
        {
            return Result<AdminMerchantDetailDto>.BadRequest("Send an end date, a number of days to add, or unlimited.");
        }

        merchant.SubscriptionReminderSentAt = null;

        var body = merchant.SubscriptionEndsAt is { } ends ? $"الاشتراك ساري لحد {LibyaTime.ToLocal(ends):yyyy-MM-dd}." : "الاشتراك ساري من غير تاريخ انتهاء.";

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForMerchant(id, NotificationTypes.SubscriptionUpdated, "تم تحديث اشتراكك", body, RelatedTypes.Merchant, id), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        merchantAccess.Forget(id);

        return Result<AdminMerchantDetailDto>.Ok((await LoadAsync(id, cancellationToken))!, "Subscription updated.");
    }

    public async Task<Result> ResetUserPasswordAsync(int id, int userId, ResetUserPasswordRequest request, CancellationToken cancellationToken = default)
    {
        return await provisioning.ResetPasswordAsync(id, userId, request.NewPassword, cancellationToken);
    }

    public async Task<Result<AdminOverviewDto>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var soon = now.AddDays(7);

        var since = now.AddDays(-30);

        var merchants = (await unitOfWork.Repository<Merchant>().Query()
            .GroupBy(merchant => 1)
            .Select(group => new
            {
                Total = group.Count(),

                Active = group.Count(merchant => merchant.Status == MerchantStatus.Active),

                Suspended = group.Count(merchant => merchant.Status == MerchantStatus.Suspended),

                Expiring = group.Count(merchant => merchant.SubscriptionEndsAt > now && merchant.SubscriptionEndsAt <= soon),

                Expired = group.Count(merchant => merchant.SubscriptionEndsAt <= now)
            })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var applications = await unitOfWork.Repository<MerchantApplication>().CountAsync(application => application.Status == MerchantApplicationStatus.Pending, cancellationToken);

        var tickets = await unitOfWork.Repository<Ticket>().CountAsync(ticket => ticket.Status == TicketStatus.Open, cancellationToken);

        var recent = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.Status == SalesInvoiceStatus.Completed && invoice.CompletedAt >= since);

        var sales = await recent.SumAsync(invoice => (decimal?)(invoice.Total - invoice.ReturnedAmount), cancellationToken) ?? 0;

        var count = await recent.CountAsync(cancellationToken);

        var top = await recent
            .GroupBy(invoice => invoice.MerchantId)
            .Select(group => new { MerchantId = group.Key, Invoices = group.Count(), Sales = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount) })
            .OrderByDescending(row => row.Sales)
            .Take(5)
            .ToListAsync(cancellationToken);

        var ids = top.Select(row => row.MerchantId).ToList();

        var names = await unitOfWork.Repository<Merchant>().Query().Where(merchant => ids.Contains(merchant.Id)).ToDictionaryAsync(merchant => merchant.Id, cancellationToken);

        var topMerchants = top
            .Where(row => names.ContainsKey(row.MerchantId))
            .Select(row => new TopMerchantDto(row.MerchantId, names[row.MerchantId].Code, names[row.MerchantId].Name, row.Invoices, Money.Round(row.Sales)))
            .ToList();

        return Result<AdminOverviewDto>.Ok(new AdminOverviewDto(merchants?.Total ?? 0, merchants?.Active ?? 0, merchants?.Suspended ?? 0, merchants?.Expiring ?? 0, merchants?.Expired ?? 0, applications, tickets, Money.Round(sales), count, topMerchants));
    }

    /// <summary>The last moment of a Libyan day, in UTC.</summary>
    public static DateTime EndOfDay(DateOnly day) => LibyaTime.ToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue)).AddTicks(-1);

    private Task<Merchant?> Tracked(int id, CancellationToken cancellationToken) => unitOfWork.Repository<Merchant>().Query(asNoTracking: false).FirstOrDefaultAsync(merchant => merchant.Id == id, cancellationToken);

    private async Task<AdminMerchantDetailDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var merchant = await unitOfWork.Repository<Merchant>().Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (merchant is null)
        {
            return null;
        }

        var invoices = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.MerchantId == id && invoice.Status == SalesInvoiceStatus.Completed);

        var since = DateTime.UtcNow.AddDays(-30);

        var totals = (await invoices
            .GroupBy(invoice => 1)
            .Select(group => new { Count = group.Count(), Sales = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount), Recent = group.Where(invoice => invoice.CompletedAt >= since).Sum(invoice => invoice.Total - invoice.ReturnedAmount), Last = group.Max(invoice => invoice.CompletedAt) })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var branches = await unitOfWork.Repository<Branch>().CountAsync(branch => branch.MerchantId == id, cancellationToken);

        var products = await unitOfWork.Repository<Product>().CountAsync(product => product.MerchantId == id, cancellationToken);

        var customers = await unitOfWork.Repository<Customer>().CountAsync(customer => customer.MerchantId == id, cancellationToken);

        var users = await unitOfWork.Repository<User>().Query()
            .Where(user => user.MerchantId == id)
            .OrderBy(user => user.Id)
            .Select(user => new { user.Id, user.FullName, user.PhoneNumber, user.Email, user.IsActive, user.LastLoginAt })
            .ToListAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToList();

        var roles = await unitOfWork.Repository<IdentityUserRole<int>>().Query()
            .Where(link => userIds.Contains(link.UserId))
            .Join(unitOfWork.Repository<Role>().Query(), link => link.RoleId, role => role.Id, (link, role) => new { Id = link.UserId, role.Name })
            .ToListAsync(cancellationToken);

        return new AdminMerchantDetailDto(
            merchant.Id,
            merchant.Code,
            merchant.Name,
            merchant.OwnerName,
            merchant.Phone,
            merchant.Email,
            merchant.City,
            merchant.Address,
            merchant.TaxNumber,
            FileUrls.MerchantLogo(merchant.Id, merchant.LogoPath, merchant.UpdatedAt),
            merchant.Status,
            merchant.SubscriptionEndsAt,
            merchant.SubscriptionEndsAt is null || merchant.SubscriptionEndsAt > DateTime.UtcNow,
            merchant.MaxBranches,
            merchant.MaxUsers,
            merchant.Notes,
            branches,
            products,
            customers,
            totals?.Count ?? 0,
            Money.Round(totals?.Sales ?? 0),
            Money.Round(totals?.Recent ?? 0),
            totals?.Last,
            users.Select(user => new MerchantUserDto(user.Id, user.FullName, user.PhoneNumber, user.Email, roles.FirstOrDefault(role => role.Id == user.Id)?.Name ?? string.Empty, user.IsActive, user.LastLoginAt)).ToList(),
            merchant.CreatedAt);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

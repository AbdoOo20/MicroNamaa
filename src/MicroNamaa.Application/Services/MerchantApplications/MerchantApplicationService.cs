using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.DTOs.MerchantApplications;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Application.Services.Notifications;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.MerchantApplications;

public class MerchantApplicationService(IUnitOfWork unitOfWork, IUserAppService userAppService, IAdminMerchantService merchants) : IMerchantApplicationService
{
    private const string NotFound = "Request was not found.";

    public async Task<Result> SubmitAsync(SubmitMerchantApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var phone = LibyanPhone.Normalize(request.Phone);

        if (phone is null)
        {
            return Result.BadRequest(LibyanPhone.InvalidMessage);
        }

        if (await unitOfWork.Repository<MerchantApplication>().AnyAsync(application => application.Phone == phone && application.Status == MerchantApplicationStatus.Pending, cancellationToken))
        {
            return Result.Conflict("We already have a request from this number. Our team will call you soon.");
        }

        var application = new MerchantApplication
        {
            BusinessName = request.BusinessName.Trim(),

            OwnerName = request.OwnerName.Trim(),

            Phone = phone,

            Email = Clean(request.Email),

            City = Clean(request.City),

            BusinessType = Clean(request.BusinessType),

            BranchesCount = request.BranchesCount,

            Notes = Clean(request.Notes)
        };

        await unitOfWork.Repository<MerchantApplication>().AddAsync(application, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.Repository<Notification>().AddAsync(NotificationFactory.ForAdmins(NotificationTypes.MerchantApplication, $"طلب اشتراك جديد: {application.BusinessName}", $"{application.OwnerName} — {application.Phone}{(application.City is null ? string.Empty : " — " + application.City)}", RelatedTypes.MerchantApplication, application.Id), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Created("Request received. Our team will contact you soon.");
    }

    public async Task<Result<PagedResult<MerchantApplicationDto>>> GetAllAsync(MerchantApplicationQuery query, CancellationToken cancellationToken = default)
    {
        var applications = unitOfWork.Repository<MerchantApplication>().Query();

        if (query.Status is { } status)
        {
            applications = applications.Where(application => application.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            applications = applications.WhereMatchesSearch(query.Search);
        }

        var page = await applications
            .OrderBy(application => application.Status)
            .ThenByDescending(application => application.Id)
            .Select(ToDto)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<MerchantApplicationDto>>.Ok(page);
    }

    public async Task<Result<MerchantApplicationDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var application = await unitOfWork.Repository<MerchantApplication>().Query().Where(item => item.Id == id).Select(ToDto).FirstOrDefaultAsync(cancellationToken);

        return application is null ? Result<MerchantApplicationDto>.NotFound(NotFound) : Result<MerchantApplicationDto>.Ok(application);
    }

    public async Task<Result<AdminMerchantDetailDto>> ApproveAsync(int id, ApproveMerchantApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var application = await unitOfWork.Repository<MerchantApplication>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            return Result<AdminMerchantDetailDto>.NotFound(NotFound);
        }

        if (application.Status != MerchantApplicationStatus.Pending)
        {
            return Result<AdminMerchantDetailDto>.Conflict("This request was already answered.");
        }

        var created = await merchants.CreateAsync(new CreateMerchantRequest
        {
            Name = application.BusinessName,

            OwnerName = application.OwnerName,

            Phone = application.Phone,

            Email = Clean(request.Email) ?? application.Email,

            OwnerPassword = request.OwnerPassword,

            City = application.City,

            SubscriptionEndsOn = request.SubscriptionEndsOn,

            MaxBranches = request.MaxBranches,

            MaxUsers = request.MaxUsers,

            Notes = application.Notes
        }, cancellationToken);

        if (!created.Succeeded)
        {
            return created;
        }

        application.Status = MerchantApplicationStatus.Approved;

        application.MerchantId = created.Data!.Id;

        application.AdminNote = Clean(request.AdminNote);

        application.ReviewedAt = DateTime.UtcNow;

        application.ReviewedBy = userAppService.UserId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<Result<MerchantApplicationDto>> RejectAsync(int id, RejectMerchantApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var application = await unitOfWork.Repository<MerchantApplication>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (application is null)
        {
            return Result<MerchantApplicationDto>.NotFound(NotFound);
        }

        if (application.Status != MerchantApplicationStatus.Pending)
        {
            return Result<MerchantApplicationDto>.Conflict("This request was already answered.");
        }

        application.Status = MerchantApplicationStatus.Rejected;

        application.AdminNote = request.AdminNote.Trim();

        application.ReviewedAt = DateTime.UtcNow;

        application.ReviewedBy = userAppService.UserId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<MerchantApplicationDto>.Ok(ToDto.Compile()(application));
    }

    private static readonly Expression<Func<MerchantApplication, MerchantApplicationDto>> ToDto = application => new MerchantApplicationDto(
        application.Id,
        application.BusinessName,
        application.OwnerName,
        application.Phone,
        application.Email,
        application.City,
        application.BusinessType,
        application.BranchesCount,
        application.Notes,
        application.Status,
        application.AdminNote,
        application.MerchantId,
        application.ReviewedAt,
        application.CreatedAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

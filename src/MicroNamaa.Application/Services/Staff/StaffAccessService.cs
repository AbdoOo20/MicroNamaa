using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Staff;

/// <summary>Reads the permissions from the database on every request, so a change or a deactivation applies at once.</summary>
public class StaffAccessService(IUnitOfWork unitOfWork, IUserAppService userAppService) : IStaffAccessService
{
    private StaffAccess? cached;

    public async Task<StaffAccess> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        if (cached is not null)
        {
            return cached;
        }

        var userId = userAppService.UserId;

        var user = userId is null
            ? null
            : await unitOfWork.Repository<User>().Query().Where(item => item.Id == userId).Select(item => new { item.IsStaff, item.IsActive }).FirstOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsStaff)
        {
            cached = StaffAccess.Owner;

            return cached;
        }

        var permissions = await unitOfWork.Repository<StaffPermission>().Query()
            .Where(item => item.UserId == userId && item.Actions != AdminActions.None)
            .ToDictionaryAsync(item => item.Area, item => item.Actions, cancellationToken);

        cached = new StaffAccess(true, user.IsActive, permissions);

        return cached;
    }
}

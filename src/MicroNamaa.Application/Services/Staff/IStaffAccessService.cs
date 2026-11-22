namespace MicroNamaa.Application.Services.Staff;

public interface IStaffAccessService
{
    /// <summary>The current admin's access, read once per request.</summary>
    Task<StaffAccess> GetCurrentAsync(CancellationToken cancellationToken = default);
}

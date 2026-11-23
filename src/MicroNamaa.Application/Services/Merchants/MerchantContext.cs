using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Merchants;

/// <summary>
/// The signed-in user's merchant and branch, read once per request. A cashier always works in their own branch;
/// an owner or a manager may pick a branch, and falls back to their own or the first active one.
/// </summary>
public class MerchantContext(IUnitOfWork unitOfWork, IUserAppService userAppService)
{
    private Merchant? merchant;

    public int MerchantId => userAppService.RequiredMerchantId;

    public async Task<Merchant> GetMerchantAsync(CancellationToken cancellationToken = default)
    {
        merchant ??= await unitOfWork.Repository<Merchant>().Query().FirstOrDefaultAsync(item => item.Id == MerchantId, cancellationToken)
            ?? throw new UnauthorizedAccessException("The merchant account was not found.");

        return merchant;
    }

    public async Task<Result<Branch>> ResolveBranchAsync(int? requestedBranchId, CancellationToken cancellationToken = default)
    {
        var branches = unitOfWork.Repository<Branch>().Query(asNoTracking: true, branch => branch.DefaultWarehouse!);

        if (userAppService.IsCashier)
        {
            if (userAppService.BranchId is not { } ownBranch)
            {
                return Result<Branch>.Forbidden("Your account is not linked to a branch. Ask the store manager.");
            }

            if (requestedBranchId is { } other && other != ownBranch)
            {
                return Result<Branch>.Forbidden("Cashiers can only sell from their own branch.");
            }

            requestedBranchId = ownBranch;
        }

        var branch = requestedBranchId is { } branchId
            ? await branches.FirstOrDefaultAsync(item => item.Id == branchId, cancellationToken)
            : userAppService.BranchId is { } mine
                ? await branches.FirstOrDefaultAsync(item => item.Id == mine && item.IsActive, cancellationToken)
                : null;

        branch ??= requestedBranchId is null ? await branches.Where(item => item.IsActive).OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken) : null;

        if (branch is null)
        {
            return Result<Branch>.NotFound("Branch was not found.");
        }

        if (!branch.IsActive)
        {
            return Result<Branch>.Conflict("This branch is turned off.");
        }

        return Result<Branch>.Ok(branch);
    }

    /// <summary>The warehouse a branch sells from: its default one, otherwise the first active warehouse of the branch.</summary>
    public async Task<Warehouse?> GetSellingWarehouseAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        if (branch.DefaultWarehouse is { Status: RecordStatus.Active } warehouse)
        {
            return warehouse;
        }

        return await unitOfWork.Repository<Warehouse>().Query()
            .Where(item => item.Status == RecordStatus.Active && (item.BranchId == branch.Id || item.BranchId == null))
            .OrderBy(item => item.BranchId == branch.Id ? 0 : 1)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}

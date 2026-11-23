using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Merchants;

public class BranchService(IUnitOfWork unitOfWork, MerchantContext context) : IBranchService
{
    private const string NotFound = "Branch was not found.";

    public async Task<Result<IReadOnlyList<BranchDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var branches = await Project(unitOfWork.Repository<Branch>().Query().OrderBy(branch => branch.Id)).ToListAsync(cancellationToken);

        return Result<IReadOnlyList<BranchDto>>.Ok(branches);
    }

    public async Task<Result<BranchDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var branch = await LoadAsync(id, cancellationToken);

        return branch is null ? Result<BranchDto>.NotFound(NotFound) : Result<BranchDto>.Ok(branch);
    }

    public async Task<Result<BranchDto>> CreateAsync(BranchRequest request, CancellationToken cancellationToken = default)
    {
        var merchant = await context.GetMerchantAsync(cancellationToken);

        var count = await unitOfWork.Repository<Branch>().CountAsync(cancellationToken: cancellationToken);

        if (merchant.MaxBranches is { } max && count >= max)
        {
            return Result<BranchDto>.Conflict($"Your plan allows {max} branches. Contact support to add more.");
        }

        if (await CheckWarehouseAsync(request.DefaultWarehouseId, cancellationToken) is { } invalid)
        {
            return Result<BranchDto>.From(invalid);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var branch = new Branch
        {
            Name = request.Name.Trim(),

            City = Clean(request.City),

            Address = Clean(request.Address),

            Phone = Clean(request.Phone),

            DefaultWarehouseId = request.DefaultWarehouseId,

            IsActive = request.IsActive,

            Registers = [new Register { Name = "Reg 01" }]
        };

        await unitOfWork.Repository<Branch>().AddAsync(branch, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.CreateWarehouse && request.DefaultWarehouseId is null)
        {
            var warehouse = new Warehouse { BranchId = branch.Id, Name = $"مخزن {branch.Name}", Location = branch.Address ?? branch.City };

            await unitOfWork.Repository<Warehouse>().AddAsync(warehouse, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            branch.DefaultWarehouseId = warehouse.Id;

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<BranchDto>.Created((await LoadAsync(branch.Id, cancellationToken))!, "Branch created.");
    }

    public async Task<Result<BranchDto>> UpdateAsync(int id, BranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await unitOfWork.Repository<Branch>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (branch is null)
        {
            return Result<BranchDto>.NotFound(NotFound);
        }

        if (await CheckWarehouseAsync(request.DefaultWarehouseId, cancellationToken) is { } invalid)
        {
            return Result<BranchDto>.From(invalid);
        }

        if (!request.IsActive && branch.IsActive && !await unitOfWork.Repository<Branch>().AnyAsync(item => item.Id != id && item.IsActive, cancellationToken))
        {
            return Result<BranchDto>.Conflict("At least one branch must stay active.");
        }

        branch.Name = request.Name.Trim();

        branch.City = Clean(request.City);

        branch.Address = Clean(request.Address);

        branch.Phone = Clean(request.Phone);

        branch.DefaultWarehouseId = request.DefaultWarehouseId;

        branch.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BranchDto>.Ok((await LoadAsync(id, cancellationToken))!, "Branch updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var branch = await unitOfWork.Repository<Branch>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (branch is null)
        {
            return Result.NotFound(NotFound);
        }

        if (await unitOfWork.Repository<SalesInvoice>().AnyAsync(invoice => invoice.BranchId == id, cancellationToken))
        {
            return Result.Conflict("This branch has invoices. Turn it off instead of deleting it.");
        }

        if (await unitOfWork.Repository<User>().AnyAsync(user => user.BranchId == id && user.MerchantId == context.MerchantId, cancellationToken))
        {
            return Result.Conflict("Move this branch's employees to another branch first.");
        }

        if (!await unitOfWork.Repository<Branch>().AnyAsync(item => item.Id != id && item.IsActive, cancellationToken))
        {
            return Result.Conflict("At least one branch must stay active.");
        }

        unitOfWork.Repository<Branch>().Remove(branch);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Branch deleted.");
    }

    public async Task<Result<RegisterDto>> AddRegisterAsync(int branchId, RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!await unitOfWork.Repository<Branch>().AnyAsync(branch => branch.Id == branchId, cancellationToken))
        {
            return Result<RegisterDto>.NotFound(NotFound);
        }

        var register = new Register { BranchId = branchId, Name = request.Name.Trim(), IsActive = request.IsActive };

        await unitOfWork.Repository<Register>().AddAsync(register, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterDto>.Created(new RegisterDto(register.Id, register.BranchId, register.Name, register.IsActive));
    }

    public async Task<Result<RegisterDto>> UpdateRegisterAsync(int branchId, int registerId, RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var register = await unitOfWork.Repository<Register>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == registerId && item.BranchId == branchId, cancellationToken);

        if (register is null)
        {
            return Result<RegisterDto>.NotFound("Register was not found.");
        }

        register.Name = request.Name.Trim();

        register.IsActive = request.IsActive;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegisterDto>.Ok(new RegisterDto(register.Id, register.BranchId, register.Name, register.IsActive));
    }

    public async Task<Result> DeleteRegisterAsync(int branchId, int registerId, CancellationToken cancellationToken = default)
    {
        var register = await unitOfWork.Repository<Register>().Query(asNoTracking: false).FirstOrDefaultAsync(item => item.Id == registerId && item.BranchId == branchId, cancellationToken);

        if (register is null)
        {
            return Result.NotFound("Register was not found.");
        }

        unitOfWork.Repository<Register>().Remove(register);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Register deleted.");
    }

    private async Task<Result?> CheckWarehouseAsync(int? warehouseId, CancellationToken cancellationToken)
    {
        if (warehouseId is { } id && !await unitOfWork.Repository<Warehouse>().AnyAsync(warehouse => warehouse.Id == id && warehouse.Status == RecordStatus.Active, cancellationToken))
        {
            return Result.BadRequest("The selling warehouse was not found or is not active.");
        }

        return null;
    }

    private Task<BranchDto?> LoadAsync(int id, CancellationToken cancellationToken) =>
        Project(unitOfWork.Repository<Branch>().Query().Where(branch => branch.Id == id)).FirstOrDefaultAsync(cancellationToken);

    private IQueryable<BranchDto> Project(IQueryable<Branch> branches)
    {
        var users = unitOfWork.Repository<User>().Query();

        return branches.Select(branch => new BranchDto(
            branch.Id,
            branch.Name,
            branch.City,
            branch.Address,
            branch.Phone,
            branch.DefaultWarehouseId,
            branch.DefaultWarehouse == null ? null : branch.DefaultWarehouse.Name,
            branch.IsActive,
            users.Count(user => user.BranchId == branch.Id),
            branch.Registers.OrderBy(register => register.Id).Select(register => new RegisterDto(register.Id, register.BranchId, register.Name, register.IsActive)).ToList(),
            branch.CreatedAt));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

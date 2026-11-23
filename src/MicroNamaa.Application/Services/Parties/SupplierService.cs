using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Parties;

public class SupplierService(IUnitOfWork unitOfWork, MerchantContext context, IDocumentNumberService numbers, BalanceLedger ledger, ImageUploads images) : ISupplierService
{
    private const string NotFound = "Supplier was not found.";

    public async Task<Result<PagedResult<SupplierListItemDto>>> GetAllAsync(SupplierQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query).OrderBy(supplier => supplier.Number).ToPagedResultAsync(Project, query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<SupplierListItemDto>>.Ok(page);
    }

    public async Task<Result<SupplierSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await unitOfWork.Repository<Supplier>().Query()
            .GroupBy(supplier => supplier.Status)
            .Select(group => new { group.Key, Count = group.Count(), Owed = group.Sum(supplier => supplier.Balance > 0 ? supplier.Balance : 0) })
            .ToListAsync(cancellationToken);

        var paid = await unitOfWork.Repository<SupplierBalanceEntry>().Query().Where(entry => entry.Type == BalanceEntryType.Payment).SumAsync(entry => (decimal?)-entry.Amount, cancellationToken) ?? 0;

        var invoices = await unitOfWork.Repository<PurchaseOrder>().CountAsync(order => order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Cancelled, cancellationToken);

        var tabs = new List<SummaryCount> { new("all", byStatus.Sum(row => row.Count)) };

        tabs.AddRange(Enum.GetValues<RecordStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), byStatus.FirstOrDefault(row => row.Key == status)?.Count ?? 0)));

        return Result<SupplierSummaryDto>.Ok(new SupplierSummaryDto(Money.Round(paid), byStatus.Sum(row => row.Count), invoices, Money.Round(byStatus.Sum(row => row.Owed)), tabs));
    }

    public async Task<Result<SupplierDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await LoadAsync(id, cancellationToken);

        return supplier is null ? Result<SupplierDetailDto>.NotFound(NotFound) : Result<SupplierDetailDto>.Ok(supplier);
    }

    public async Task<Result<SupplierDetailDto>> CreateAsync(SupplierRequest request, CancellationToken cancellationToken = default)
    {
        var validated = await ValidateAsync(null, request, cancellationToken);

        if (!validated.Succeeded)
        {
            return Result<SupplierDetailDto>.From(validated);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var supplier = new Supplier { Number = await numbers.NextAsync(context.MerchantId, DocumentType.Supplier, cancellationToken) };

        Apply(supplier, request, validated.Data);

        await unitOfWork.Repository<Supplier>().AddAsync(supplier, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<SupplierDetailDto>.Created((await LoadAsync(supplier.Id, cancellationToken))!, "Supplier created.");
    }

    public async Task<Result<SupplierDetailDto>> UpdateAsync(int id, SupplierRequest request, CancellationToken cancellationToken = default)
    {
        var supplier = await Tracked(id, cancellationToken);

        if (supplier is null)
        {
            return Result<SupplierDetailDto>.NotFound(NotFound);
        }

        var validated = await ValidateAsync(id, request, cancellationToken);

        if (!validated.Succeeded)
        {
            return Result<SupplierDetailDto>.From(validated);
        }

        Apply(supplier, request, validated.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SupplierDetailDto>.Ok((await LoadAsync(id, cancellationToken))!, "Supplier updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await Tracked(id, cancellationToken);

        if (supplier is null)
        {
            return Result.NotFound(NotFound);
        }

        if (supplier.Balance != 0)
        {
            return Result.Conflict("The merchant still has a balance with this supplier. Settle it or archive the supplier instead.");
        }

        if (await unitOfWork.Repository<PurchaseOrder>().AnyAsync(order => order.SupplierId == id, cancellationToken))
        {
            return Result.Conflict("This supplier has purchase orders. Archive it instead of deleting it.");
        }

        unitOfWork.Repository<Supplier>().Remove(supplier);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Supplier deleted.");
    }

    public async Task<Result<SupplierDetailDto>> SetLogoAsync(int id, FileUpload? file, CancellationToken cancellationToken = default)
    {
        var supplier = await Tracked(id, cancellationToken);

        if (supplier is null)
        {
            return Result<SupplierDetailDto>.NotFound(NotFound);
        }

        if (file is null)
        {
            await images.DeleteAsync(supplier.LogoPath, cancellationToken);

            supplier.LogoPath = null;
        }

        else
        {
            var saved = await images.ReplaceAsync(supplier.LogoPath, file, "suppliers", "logo", cancellationToken);

            if (!saved.Succeeded)
            {
                return Result<SupplierDetailDto>.From(saved);
            }

            supplier.LogoPath = saved.Data;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SupplierDetailDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<PagedResult<BalanceEntryDto>>> GetStatementAsync(int id, BalanceEntryQuery query, CancellationToken cancellationToken = default)
    {
        if (!await unitOfWork.Repository<Supplier>().AnyAsync(supplier => supplier.Id == id, cancellationToken))
        {
            return Result<PagedResult<BalanceEntryDto>>.NotFound(NotFound);
        }

        var entries = unitOfWork.Repository<SupplierBalanceEntry>().Query().Where(entry => entry.SupplierId == id);

        if (query.Type is { } type)
        {
            entries = entries.Where(entry => entry.Type == type);
        }

        if (query.From is not null || query.To is not null)
        {
            var range = DateRange.FromLocalDates(query.From, query.To);

            entries = entries.Where(entry => entry.CreatedAt >= range.From && entry.CreatedAt < range.To);
        }

        var users = unitOfWork.Repository<User>().Query();

        var page = await entries
            .OrderByDescending(entry => entry.Id)
            .ToPagedResultAsync(
                pageQuery => pageQuery.Select(entry => new BalanceEntryDto(entry.Id, entry.Type, entry.Amount, entry.BalanceAfter, entry.PaymentMethod, entry.Reference, entry.PurchaseOrderId, entry.Note, users.Where(user => user.Id == entry.CreatedBy).Select(user => user.FullName).FirstOrDefault(), entry.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<BalanceEntryDto>>.Ok(page);
    }

    public async Task<Result<BalanceEntryDto>> PayAsync(int id, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Method == PaymentMethod.Deferred)
        {
            return Result<BalanceEntryDto>.BadRequest("A payment must be cash, card or bank transfer.");
        }

        var supplier = await Tracked(id, cancellationToken);

        if (supplier is null)
        {
            return Result<BalanceEntryDto>.NotFound(NotFound);
        }

        var entry = await ledger.SupplierAsync(supplier, BalanceEntryType.Payment, -request.Amount, Clean(request.Reference), Clean(request.Note), request.Method, cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BalanceEntryDto>.Created(ToDto(entry), "Payment recorded.");
    }

    public async Task<Result<BalanceEntryDto>> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount == 0)
        {
            return Result<BalanceEntryDto>.BadRequest("The amount cannot be zero.");
        }

        var supplier = await Tracked(id, cancellationToken);

        if (supplier is null)
        {
            return Result<BalanceEntryDto>.NotFound(NotFound);
        }

        var entry = await ledger.SupplierAsync(supplier, BalanceEntryType.Adjustment, request.Amount, null, request.Note.Trim(), cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BalanceEntryDto>.Created(ToDto(entry), "Balance adjusted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(SupplierQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Project(Filter(query).OrderBy(supplier => supplier.Number)).ToListAsync(cancellationToken);

        var csv = new CsvFile("Code", "Name", "Contact person", "Phone", "Email", "Balance (LYD)", "Purchases", "Last purchase", "Status", "Created");

        foreach (var row in rows)
        {
            csv.Row(row.Code, row.Name, row.ContactPerson, row.Phone, row.Email, row.Balance, row.PurchasesCount, row.LastPurchaseAt, row.Status, row.CreatedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("suppliers"));
    }

    private IQueryable<Supplier> Filter(SupplierQuery query)
    {
        var suppliers = unitOfWork.Repository<Supplier>().Query();

        if (query.Status is { } status)
        {
            suppliers = suppliers.Where(supplier => supplier.Status == status);
        }

        if (query.OwedOnly)
        {
            suppliers = suppliers.Where(supplier => supplier.Balance > 0);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            suppliers = suppliers.WhereMatchesSearch(query.Search);
        }

        return suppliers;
    }

    private IQueryable<SupplierListItemDto> Project(IQueryable<Supplier> suppliers)
    {
        var orders = unitOfWork.Repository<PurchaseOrder>().Query().Where(order => order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Cancelled);

        return suppliers.Select(supplier => new SupplierListItemDto(
            supplier.Id,
            Supplier.FormatCode(supplier.Number),
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.LogoPath == null ? null : FileUrls.SupplierLogo(supplier.Id, supplier.LogoPath, supplier.UpdatedAt ?? supplier.CreatedAt),
            supplier.Balance,
            orders.Count(order => order.SupplierId == supplier.Id),
            orders.Where(order => order.SupplierId == supplier.Id).Max(order => (DateTime?)order.OrderDate),
            supplier.Status,
            supplier.CreatedAt));
    }

    private async Task<SupplierDetailDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var supplier = await unitOfWork.Repository<Supplier>().Query().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (supplier is null)
        {
            return null;
        }

        var orders = unitOfWork.Repository<PurchaseOrder>().Query().Where(order => order.SupplierId == id && order.Status != PurchaseOrderStatus.Draft && order.Status != PurchaseOrderStatus.Cancelled);

        var count = await orders.CountAsync(cancellationToken);

        var last = await orders.MaxAsync(order => (DateTime?)order.OrderDate, cancellationToken);

        var purchased = await orders.Where(order => order.Status == PurchaseOrderStatus.Delivered).SumAsync(order => (decimal?)order.Total, cancellationToken) ?? 0;

        var paid = await unitOfWork.Repository<SupplierBalanceEntry>().Query().Where(entry => entry.SupplierId == id && entry.Type == BalanceEntryType.Payment).SumAsync(entry => (decimal?)-entry.Amount, cancellationToken) ?? 0;

        return new SupplierDetailDto(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.ContactPerson,
            supplier.Phone,
            supplier.Email,
            supplier.City,
            supplier.Address,
            supplier.TaxNumber,
            FileUrls.SupplierLogo(supplier.Id, supplier.LogoPath, supplier.UpdatedAt ?? supplier.CreatedAt),
            supplier.Balance,
            supplier.Status,
            supplier.Notes,
            count,
            Money.Round(purchased),
            Money.Round(paid),
            last,
            supplier.CreatedAt);
    }

    private async Task<Result<string?>> ValidateAsync(int? id, SupplierRequest request, CancellationToken cancellationToken)
    {
        // Suppliers can be landlines or foreign numbers, so the phone is only cleaned, not checked as a Libyan mobile.
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : LibyanPhone.Normalize(request.Phone) ?? request.Phone.Trim();

        var name = request.Name.Trim();

        if (await unitOfWork.Repository<Supplier>().AnyAsync(supplier => supplier.Id != id && supplier.Name == name, cancellationToken))
        {
            return Result<string?>.Conflict("A supplier with this name already exists.");
        }

        return Result<string?>.Ok(phone);
    }

    private static void Apply(Supplier supplier, SupplierRequest request, string? phone)
    {
        supplier.Name = request.Name.Trim();

        supplier.ContactPerson = Clean(request.ContactPerson);

        supplier.Phone = phone;

        supplier.Email = Clean(request.Email);

        supplier.City = Clean(request.City);

        supplier.Address = Clean(request.Address);

        supplier.TaxNumber = Clean(request.TaxNumber);

        supplier.Status = request.Status;

        supplier.Notes = Clean(request.Notes);
    }

    private Task<Supplier?> Tracked(int id, CancellationToken cancellationToken) => unitOfWork.Repository<Supplier>().Query(asNoTracking: false).FirstOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);

    private static BalanceEntryDto ToDto(SupplierBalanceEntry entry) => new(entry.Id, entry.Type, entry.Amount, entry.BalanceAfter, entry.PaymentMethod, entry.Reference, entry.PurchaseOrderId, entry.Note, null, entry.CreatedAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

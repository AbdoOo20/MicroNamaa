using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Parties;

using MicroNamaa.Application.Services.Common;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Parties;

public class CustomerService(IUnitOfWork unitOfWork, MerchantContext context, IDocumentNumberService numbers, BalanceLedger ledger, ImageUploads images) : ICustomerService
{
    private const string NotFound = "Customer was not found.";

    public async Task<Result<PagedResult<CustomerListItemDto>>> GetAllAsync(CustomerQuery query, CancellationToken cancellationToken = default)
    {
        var page = await Filter(query).OrderByDescending(customer => customer.Id).ToPagedResultAsync(Project, query.Page, query.PageSize, cancellationToken);

        return Result<PagedResult<CustomerListItemDto>>.Ok(page);
    }

    public async Task<Result<CustomerSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var byStatus = await unitOfWork.Repository<Customer>().Query()
            .GroupBy(customer => customer.Status)
            .Select(group => new { group.Key, Count = group.Count(), Owed = group.Sum(customer => customer.Balance > 0 ? customer.Balance : 0) })
            .ToListAsync(cancellationToken);

        var sales = await unitOfWork.Repository<SalesInvoice>().Query()
            .Where(invoice => invoice.Status == SalesInvoiceStatus.Completed && invoice.CustomerId != null)
            .SumAsync(invoice => (decimal?)(invoice.Total - invoice.ReturnedAmount), cancellationToken) ?? 0;

        var tabs = new List<SummaryCount> { new("all", byStatus.Sum(row => row.Count)) };

        tabs.AddRange(Enum.GetValues<RecordStatus>().Select(status => new SummaryCount(status.ToString().ToLowerInvariant(), byStatus.FirstOrDefault(row => row.Key == status)?.Count ?? 0)));

        return Result<CustomerSummaryDto>.Ok(new CustomerSummaryDto(byStatus.Sum(row => row.Count), Money.Round(sales), Money.Round(byStatus.Sum(row => row.Owed)), tabs));
    }

    public async Task<Result<CustomerDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await LoadAsync(id, cancellationToken);

        return customer is null ? Result<CustomerDetailDto>.NotFound(NotFound) : Result<CustomerDetailDto>.Ok(customer);
    }

    public async Task<Result<CustomerDetailDto>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var validated = await ValidateAsync(null, request, cancellationToken);

        if (!validated.Succeeded)
        {
            return Result<CustomerDetailDto>.From(validated);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        var customer = new Customer { Number = await numbers.NextAsync(context.MerchantId, DocumentType.Customer, cancellationToken) };

        Apply(customer, request, validated.Data);

        await unitOfWork.Repository<Customer>().AddAsync(customer, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await unitOfWork.CommitTransactionAsync(cancellationToken);

        return Result<CustomerDetailDto>.Created((await LoadAsync(customer.Id, cancellationToken))!, "Customer created.");
    }

    public async Task<Result<CustomerDetailDto>> UpdateAsync(int id, CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await Tracked(id, cancellationToken);

        if (customer is null)
        {
            return Result<CustomerDetailDto>.NotFound(NotFound);
        }

        var validated = await ValidateAsync(id, request, cancellationToken);

        if (!validated.Succeeded)
        {
            return Result<CustomerDetailDto>.From(validated);
        }

        Apply(customer, request, validated.Data);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CustomerDetailDto>.Ok((await LoadAsync(id, cancellationToken))!, "Customer updated.");
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await Tracked(id, cancellationToken);

        if (customer is null)
        {
            return Result.NotFound(NotFound);
        }

        if (customer.Balance != 0)
        {
            return Result.Conflict("This customer still has a balance. Settle it or archive the customer instead.");
        }

        unitOfWork.Repository<Customer>().Remove(customer);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok("Customer deleted.");
    }

    public async Task<Result<CustomerDetailDto>> SetAvatarAsync(int id, FileUpload? file, CancellationToken cancellationToken = default)
    {
        var customer = await Tracked(id, cancellationToken);

        if (customer is null)
        {
            return Result<CustomerDetailDto>.NotFound(NotFound);
        }

        if (file is null)
        {
            await images.DeleteAsync(customer.AvatarPath, cancellationToken);

            customer.AvatarPath = null;
        }

        else
        {
            var saved = await images.ReplaceAsync(customer.AvatarPath, file, "customers", "photo", cancellationToken);

            if (!saved.Succeeded)
            {
                return Result<CustomerDetailDto>.From(saved);
            }

            customer.AvatarPath = saved.Data;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CustomerDetailDto>.Ok((await LoadAsync(id, cancellationToken))!);
    }

    public async Task<Result<PagedResult<BalanceEntryDto>>> GetStatementAsync(int id, BalanceEntryQuery query, CancellationToken cancellationToken = default)
    {
        if (!await unitOfWork.Repository<Customer>().AnyAsync(customer => customer.Id == id, cancellationToken))
        {
            return Result<PagedResult<BalanceEntryDto>>.NotFound(NotFound);
        }

        var entries = unitOfWork.Repository<CustomerBalanceEntry>().Query().Where(entry => entry.CustomerId == id);

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
                pageQuery => pageQuery.Select(entry => new BalanceEntryDto(entry.Id, entry.Type, entry.Amount, entry.BalanceAfter, entry.PaymentMethod, entry.Reference, entry.SalesInvoiceId ?? entry.SalesReturnId, entry.Note, users.Where(user => user.Id == entry.CreatedBy).Select(user => user.FullName).FirstOrDefault(), entry.CreatedAt)),
                query.Page,
                query.PageSize,
                cancellationToken);

        return Result<PagedResult<BalanceEntryDto>>.Ok(page);
    }

    public async Task<Result<BalanceEntryDto>> ReceivePaymentAsync(int id, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Method == PaymentMethod.Deferred)
        {
            return Result<BalanceEntryDto>.BadRequest("A payment must be cash, card or bank transfer.");
        }

        var customer = await Tracked(id, cancellationToken);

        if (customer is null)
        {
            return Result<BalanceEntryDto>.NotFound(NotFound);
        }

        var entry = await ledger.CustomerAsync(customer, BalanceEntryType.Payment, -request.Amount, Clean(request.Reference), Clean(request.Note), request.Method, cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BalanceEntryDto>.Created(ToDto(entry), "Payment recorded.");
    }

    public async Task<Result<BalanceEntryDto>> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount == 0)
        {
            return Result<BalanceEntryDto>.BadRequest("The amount cannot be zero.");
        }

        var customer = await Tracked(id, cancellationToken);

        if (customer is null)
        {
            return Result<BalanceEntryDto>.NotFound(NotFound);
        }

        var entry = await ledger.CustomerAsync(customer, BalanceEntryType.Adjustment, request.Amount, null, request.Note.Trim(), cancellationToken: cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<BalanceEntryDto>.Created(ToDto(entry), "Balance adjusted.");
    }

    public async Task<Result<FileDownload>> ExportAsync(CustomerQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await Project(Filter(query).OrderBy(customer => customer.Number)).ToListAsync(cancellationToken);

        var csv = new CsvFile("Code", "Name", "Phone", "Email", "Branch", "Orders", "Lifetime value (LYD)", "Balance (LYD)", "Points", "Status", "Last purchase", "Created");

        foreach (var row in rows)
        {
            csv.Row(row.Code, row.Name, row.Phone, row.Email, row.BranchName, row.OrdersCount, row.LifetimeValue, row.Balance, row.LoyaltyPoints, row.Status, row.LastPurchaseAt, row.CreatedAt);
        }

        return Result<FileDownload>.Ok(csv.ToDownload("customers"));
    }

    private IQueryable<Customer> Filter(CustomerQuery query)
    {
        var customers = unitOfWork.Repository<Customer>().Query();

        if (query.Status is { } status)
        {
            customers = customers.Where(customer => customer.Status == status);
        }

        if (query.BranchId is { } branchId)
        {
            customers = customers.Where(customer => customer.BranchId == branchId);
        }

        if (query.OwingOnly)
        {
            customers = customers.Where(customer => customer.Balance > 0);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            customers = customers.WhereMatchesSearch(query.Search);
        }

        return customers;
    }

    private IQueryable<CustomerListItemDto> Project(IQueryable<Customer> customers)
    {
        var invoices = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.Status == SalesInvoiceStatus.Completed);

        return customers
            .Select(customer => new
            {
                Customer = customer,

                BranchName = customer.Branch == null ? null : customer.Branch.Name,

                Orders = invoices.Count(invoice => invoice.CustomerId == customer.Id),

                Value = invoices.Where(invoice => invoice.CustomerId == customer.Id).Sum(invoice => (decimal?)(invoice.Total - invoice.ReturnedAmount)) ?? 0,

                Last = invoices.Where(invoice => invoice.CustomerId == customer.Id).Max(invoice => invoice.CompletedAt)
            })
            .Select(row => new CustomerListItemDto(
                row.Customer.Id,
                Customer.FormatCode(row.Customer.Number),
                row.Customer.Name,
                row.Customer.Phone,
                row.Customer.Email,
                row.Customer.AvatarPath == null ? null : FileUrls.CustomerAvatar(row.Customer.Id, row.Customer.AvatarPath, row.Customer.UpdatedAt ?? row.Customer.CreatedAt),
                row.Customer.BranchId,
                row.BranchName,
                row.Orders,
                row.Value,
                row.Customer.Balance,
                row.Customer.LoyaltyPoints,
                row.Customer.Status,
                row.Last,
                row.Customer.CreatedAt));
    }

    private async Task<CustomerDetailDto?> LoadAsync(int id, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.Repository<Customer>().Query()
            .Where(item => item.Id == id)
            .Select(item => new { Customer = item, BranchName = item.Branch == null ? null : item.Branch.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return null;
        }

        var invoices = unitOfWork.Repository<SalesInvoice>().Query().Where(invoice => invoice.CustomerId == id);

        var totals = (await invoices
            .Where(invoice => invoice.Status == SalesInvoiceStatus.Completed)
            .GroupBy(invoice => 1)
            .Select(group => new { Orders = group.Count(), Value = group.Sum(invoice => invoice.Total - invoice.ReturnedAmount), Last = group.Max(invoice => invoice.CompletedAt) })
            .ToListAsync(cancellationToken)).FirstOrDefault();

        var recent = await invoices
            .Where(invoice => invoice.Status != SalesInvoiceStatus.Draft)
            .OrderByDescending(invoice => invoice.Id)
            .Take(10)
            .Select(invoice => new { invoice.Id, invoice.Number, invoice.Status, invoice.Total, invoice.DueAmount, invoice.ReturnedAmount, invoice.PaymentMethod, invoice.CreatedAt })
            .ToListAsync(cancellationToken);

        var entity = customer.Customer;

        var orders = totals?.Orders ?? 0;

        var value = Money.Round(totals?.Value ?? 0);

        return new CustomerDetailDto(
            entity.Id,
            entity.Code,
            entity.Name,
            entity.Phone,
            entity.Email,
            entity.City,
            entity.Address,
            FileUrls.CustomerAvatar(entity.Id, entity.AvatarPath, entity.UpdatedAt ?? entity.CreatedAt),
            entity.BranchId,
            customer.BranchName,
            entity.Balance,
            entity.CreditLimit,
            entity.LoyaltyPoints,
            entity.Status,
            entity.Notes,
            orders,
            value,
            orders == 0 ? 0 : Money.Round(value / orders),
            totals?.Last,
            recent.Select(invoice => new CustomerInvoiceDto(invoice.Id, SalesInvoice.FormatCode(invoice.Number), invoice.Status, invoice.Total, invoice.DueAmount, invoice.ReturnedAmount, invoice.PaymentMethod, invoice.CreatedAt)).ToList(),
            entity.CreatedAt);
    }

    private async Task<Result<string?>> ValidateAsync(int? id, CustomerRequest request, CancellationToken cancellationToken)
    {
        if (!LibyanPhone.TryNormalizeOptional(request.Phone, out var phone))
        {
            return Result<string?>.BadRequest(LibyanPhone.InvalidMessage);
        }

        if (phone is not null && await unitOfWork.Repository<Customer>().AnyAsync(customer => customer.Id != id && customer.Phone == phone, cancellationToken))
        {
            return Result<string?>.Conflict("Another customer already has this mobile number.");
        }

        if (request.BranchId is { } branchId && !await unitOfWork.Repository<Branch>().AnyAsync(branch => branch.Id == branchId, cancellationToken))
        {
            return Result<string?>.BadRequest("Branch was not found.");
        }

        return Result<string?>.Ok(phone);
    }

    private static void Apply(Customer customer, CustomerRequest request, string? phone)
    {
        customer.Name = request.Name.Trim();

        customer.Phone = phone;

        customer.Email = Clean(request.Email);

        customer.City = Clean(request.City);

        customer.Address = Clean(request.Address);

        customer.BranchId = request.BranchId;

        customer.CreditLimit = request.CreditLimit;

        customer.Status = request.Status;

        customer.Notes = Clean(request.Notes);
    }

    private Task<Customer?> Tracked(int id, CancellationToken cancellationToken) => unitOfWork.Repository<Customer>().Query(asNoTracking: false).FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

    private static BalanceEntryDto ToDto(CustomerBalanceEntry entry) => new(entry.Id, entry.Type, entry.Amount, entry.BalanceAfter, entry.PaymentMethod, entry.Reference, entry.SalesInvoiceId ?? entry.SalesReturnId, entry.Note, null, entry.CreatedAt);

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Parties;

/// <summary>
/// Changes a customer's or a supplier's balance and writes the statement line with the balance after it.
/// The entity must be tracked; the caller saves.
/// </summary>
public class BalanceLedger(IUnitOfWork unitOfWork)
{
    /// <summary>Adds a statement line. A positive amount adds to what the customer owes, a negative one takes from it.</summary>
    public async Task<CustomerBalanceEntry> CustomerAsync(Customer customer, BalanceEntryType type, decimal amount, string? reference, string? note = null, PaymentMethod? method = null, long? invoiceId = null, long? returnId = null, CancellationToken cancellationToken = default)
    {
        customer.Balance = Money.Round(customer.Balance + amount);

        var entry = new CustomerBalanceEntry
        {
            MerchantId = customer.MerchantId,

            CustomerId = customer.Id,

            Type = type,

            Amount = Money.Round(amount),

            BalanceAfter = customer.Balance,

            PaymentMethod = method,

            SalesInvoiceId = invoiceId,

            SalesReturnId = returnId,

            Reference = reference,

            Note = note
        };

        await unitOfWork.Repository<CustomerBalanceEntry>().AddAsync(entry, cancellationToken);

        return entry;
    }

    /// <summary>Adds a statement line. A positive amount adds to what the merchant owes the supplier, a negative one takes from it.</summary>
    public async Task<SupplierBalanceEntry> SupplierAsync(Supplier supplier, BalanceEntryType type, decimal amount, string? reference, string? note = null, PaymentMethod? method = null, long? purchaseOrderId = null, CancellationToken cancellationToken = default)
    {
        supplier.Balance = Money.Round(supplier.Balance + amount);

        var entry = new SupplierBalanceEntry
        {
            MerchantId = supplier.MerchantId,

            SupplierId = supplier.Id,

            Type = type,

            Amount = Money.Round(amount),

            BalanceAfter = supplier.Balance,

            PaymentMethod = method,

            PurchaseOrderId = purchaseOrderId,

            Reference = reference,

            Note = note
        };

        await unitOfWork.Repository<SupplierBalanceEntry>().AddAsync(entry, cancellationToken);

        return entry;
    }
}

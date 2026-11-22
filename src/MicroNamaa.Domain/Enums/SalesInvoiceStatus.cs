namespace MicroNamaa.Domain.Enums;

public enum SalesInvoiceStatus
{
    /// <summary>Saved or held at the POS; it does not touch stock, balances or points.</summary>
    Draft = 1,

    Completed = 2,

    /// <summary>A completed invoice voided by a manager: stock, balance and points were put back.</summary>
    Cancelled = 3
}

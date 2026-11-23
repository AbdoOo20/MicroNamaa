namespace MicroNamaa.Domain.Enums;

/// <summary>A line in a customer's or a supplier's statement.</summary>
public enum BalanceEntryType
{
    Invoice = 1,

    Payment = 2,

    Return = 3,

    Cancellation = 4,

    Adjustment = 5
}

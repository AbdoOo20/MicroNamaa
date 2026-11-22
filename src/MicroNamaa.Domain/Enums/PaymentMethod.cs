namespace MicroNamaa.Domain.Enums;

public enum PaymentMethod
{
    Cash = 1,

    Card = 2,

    /// <summary>On account: the unpaid part is added to the customer's balance.</summary>
    Deferred = 3,

    BankTransfer = 4
}

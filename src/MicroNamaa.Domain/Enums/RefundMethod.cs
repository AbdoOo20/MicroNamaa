namespace MicroNamaa.Domain.Enums;

public enum RefundMethod
{
    Cash = 1,

    Card = 2,

    /// <summary>Taken off what the customer owes (or kept as credit).</summary>
    CustomerBalance = 3
}

namespace MicroNamaa.Domain.Enums;

/// <summary>Documents numbered per merchant (INV-000001, PO-000001, RET-000001, C-0001, SUP-001).</summary>
public enum DocumentType
{
    SalesInvoice = 1,

    PurchaseOrder = 2,

    SalesReturn = 3,

    Customer = 4,

    Supplier = 5
}

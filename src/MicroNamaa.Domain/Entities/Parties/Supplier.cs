using MicroNamaa.Domain.Common;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

public class Supplier : BaseEntity<int>, IMerchantOwned, ISearchable
{
    public int MerchantId { get; set; }

    public long Number { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? ContactPerson { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? City { get; set; }

    public string? Address { get; set; }

    public string? TaxNumber { get; set; }

    public string? LogoPath { get; set; }

    /// <summary>What the merchant owes the supplier (received purchases minus payments).</summary>
    public decimal Balance { get; set; }

    public RecordStatus Status { get; set; } = RecordStatus.Active;

    public string? Notes { get; set; }

    public string SearchText { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public string Code => FormatCode(Number);

    public static string FormatCode(long number) => $"SUP-{number:000}";

    public string BuildSearchText() => ArabicSearch.Build(Code, Name, ContactPerson, Phone, Email, City);
}

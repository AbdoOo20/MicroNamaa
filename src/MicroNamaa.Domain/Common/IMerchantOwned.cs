namespace MicroNamaa.Domain.Common;

/// <summary>
/// A row that belongs to one merchant (tenant). The DbContext only shows a merchant user their own rows,
/// fills <see cref="MerchantId" /> on new rows and refuses to save a row of another merchant.
/// </summary>
public interface IMerchantOwned
{
    int MerchantId { get; set; }
}

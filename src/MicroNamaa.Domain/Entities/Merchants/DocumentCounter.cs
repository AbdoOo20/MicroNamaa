using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Domain.Entities;

/// <summary>The last number given to a document type of one merchant; incremented atomically.</summary>
public class DocumentCounter
{
    public int MerchantId { get; set; }

    public DocumentType Type { get; set; }

    public long LastNumber { get; set; }
}

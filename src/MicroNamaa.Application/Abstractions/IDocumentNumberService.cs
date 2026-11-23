using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Abstractions;

/// <summary>Gives the next number of a document type for one merchant (1, 2, 3...), safe under concurrent sales.</summary>
public interface IDocumentNumberService
{
    Task<long> NextAsync(int merchantId, DocumentType type, CancellationToken cancellationToken = default);
}

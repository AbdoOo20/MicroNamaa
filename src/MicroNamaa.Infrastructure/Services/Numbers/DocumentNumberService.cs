using System.Data;

using Microsoft.Data.SqlClient;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Domain.Enums;

using MicroNamaa.Infrastructure.Persistence;

namespace MicroNamaa.Infrastructure.Services.Numbers;

/// <summary>
/// Takes the next number from <c>DocumentCounters</c> in one locked statement, inside the caller's transaction when there is one,
/// so two cashiers completing a sale at the same moment never get the same invoice number.
/// </summary>
public class DocumentNumberService(AppDbContext context) : IDocumentNumberService
{
    private const string NextNumberSql = """
        SET NOCOUNT ON;
        UPDATE [DocumentCounters] WITH (UPDLOCK, HOLDLOCK)
        SET @next = [LastNumber] = [LastNumber] + 1
        WHERE [MerchantId] = @merchant AND [Type] = @type;
        IF @@ROWCOUNT = 0
        BEGIN
            INSERT INTO [DocumentCounters] ([MerchantId], [Type], [LastNumber]) VALUES (@merchant, @type, 1);
            SET @next = 1;
        END
        """;

    public async Task<long> NextAsync(int merchantId, DocumentType type, CancellationToken cancellationToken = default)
    {
        var next = new SqlParameter("@next", SqlDbType.BigInt) { Direction = ParameterDirection.Output };

        await context.Database.ExecuteSqlRawAsync(NextNumberSql, [new SqlParameter("@merchant", merchantId), new SqlParameter("@type", (int)type), next], cancellationToken);

        return (long)next.Value;
    }
}

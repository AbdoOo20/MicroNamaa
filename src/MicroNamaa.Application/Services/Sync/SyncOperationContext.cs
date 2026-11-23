using System.ComponentModel.DataAnnotations;

using System.Text.Json;

namespace MicroNamaa.Application.Services.Sync;

/// <summary>One operation being applied: its body and target, with "@keys" already replaced by server ids.</summary>
public class SyncOperationContext(JsonElement? payload, long? targetId, DateTime? occurredAt, string? clientKey)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DateTime? OccurredAt { get; } = occurredAt;

    public string? ClientKey { get; } = clientKey;

    public int Id => (int)(targetId ?? throw new SyncInputException(["targetId is required for this operation."]));

    public long LongId => targetId ?? throw new SyncInputException(["targetId is required for this operation."]);

    /// <summary>Reads the payload as the online endpoint's request and checks it the same way.</summary>
    public T Body<T>() where T : class
    {
        if (payload is not { ValueKind: JsonValueKind.Object } json)
        {
            throw new SyncInputException(["payload is required for this operation."]);
        }

        T? body;

        try
        {
            body = json.Deserialize<T>(JsonOptions);
        }

        catch (JsonException exception)
        {
            throw new SyncInputException([$"payload is not valid: {exception.Message}"]);
        }

        if (body is null)
        {
            throw new SyncInputException(["payload is required for this operation."]);
        }

        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(body, new ValidationContext(body), results, validateAllProperties: true))
        {
            throw new SyncInputException(results.Select(result => result.ErrorMessage ?? "Invalid value.").ToList());
        }

        return body;
    }
}

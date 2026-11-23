namespace MicroNamaa.Application.Services.Sync;

/// <summary>An operation's body or target could not be read; it fails with 400 without stopping the push.</summary>
public class SyncInputException(IReadOnlyList<string> errors) : Exception(string.Join(" ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

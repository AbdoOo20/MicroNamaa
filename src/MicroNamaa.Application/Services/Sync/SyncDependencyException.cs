namespace MicroNamaa.Application.Services.Sync;

/// <summary>The operation uses "@key" of a record whose creation failed or was never sent.</summary>
public class SyncDependencyException(string key) : Exception($"\"@{key}\" was not created on the server; send the operation that creates it first.")
{
    public string Key { get; } = key;
}

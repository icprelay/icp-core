namespace Icp.Storage;

public sealed class StorageOptions
{
    public string? Domain { get; init; }

    public string? ServiceUri { get; init; }

    public bool CreateContainerIfMissing { get; init; } = true;
}

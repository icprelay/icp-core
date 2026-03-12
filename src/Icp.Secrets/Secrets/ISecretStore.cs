namespace Icp.Secrets;

public interface ISecretStore
{
    Task<string> SetInstanceSecretAsync(
        Guid instanceId,
        string key,
        string value,
        CancellationToken ct);
}

using Azure.Security.KeyVault.Secrets;

namespace Icp.Secrets;

public sealed class KeyVaultSecretStore(SecretClient client, KeyVaultOptions options) : ISecretStore
{
    public async Task<string> SetInstanceSecretAsync(
        Guid instanceId,
        string key,
        string value,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            throw new ArgumentException("instanceId is required", nameof(instanceId));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("key is required", nameof(key));

        var secretName = BuildSecretName(instanceId, key);

        await client.SetSecretAsync(secretName, value, ct);

        // Storing only a reference in DB. Use a stable reference format.
        return $"keyvault:{client.VaultUri};secret={secretName}";
    }

    private string BuildSecretName(Guid instanceId, string key)
    {
        var safeKey = key.Trim().ToLowerInvariant().Replace('_', '-');
        return $"{options.SecretNamePrefix}-instance-{instanceId:N}-{safeKey}";
    }
}

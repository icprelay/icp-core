namespace Icp.Secrets;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    public string VaultUri { get; init; } = string.Empty;

    public string SecretNamePrefix { get; init; } = "icp";
}

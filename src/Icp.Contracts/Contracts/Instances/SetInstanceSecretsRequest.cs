namespace Icp.Contracts.Instances;

public sealed record SetInstanceSecretsRequest(
    Dictionary<string, string> Secrets);

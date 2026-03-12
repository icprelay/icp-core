namespace Icp.Contracts.IntegrationTargets;

public sealed record IntegrationTargetResponse(
    string Name,
    string ParametersTemplateJson,
    string SecretsTemplateJson,
    string Availability,
    string DisplayName,
    string IconKey);

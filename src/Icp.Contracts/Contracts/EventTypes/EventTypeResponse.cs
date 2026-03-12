namespace Icp.Contracts.EventTypes;

public sealed record EventTypeResponse(
    string Name,
    string ParametersTemplateJson,
    string DisplayName,
    string IconKey);

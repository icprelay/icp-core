namespace Icp.Contracts.Runs;

public sealed record RunResponse(
    Guid RunId,
    Guid InstanceId,
    string CorrelationId,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string? Error,
    string? OutputFullBlobPath,
    string? OutputContentType,
    DateTimeOffset CreatedAt,
    string? TriggerType = null,
    string? SubscribedEventType = null,
    string? IntegrationTarget = null);

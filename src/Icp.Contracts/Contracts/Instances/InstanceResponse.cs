namespace Icp.Contracts.Instances;

public sealed record InstanceResponse(
    Guid InstanceId,
    string CustomerId,
    string CustomerName,
    string IntegrationTarget,
    string SubscribedEventType,
    bool Enabled,
    string DisplayName,
    string IntegrationTargetParametersJson,
    string EventParametersJson,
    string SecretRefsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string TriggerType,
    string? ScheduleCron,
    string? ScheduleTimeZone,
    DateTimeOffset? NextDueAtUtc,
    int ScheduleVersion);

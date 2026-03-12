namespace Icp.Contracts.Instances;

public sealed record UpdateInstanceRequest(
    string IntegrationTarget,
    string SubscribedEventType,
    string IntegrationTargetParametersJson,
    string EventParametersJson,
    string SecretRefsJson,
    string? DisplayName,
    string TriggerType = "Event",
    string? ScheduleCron = null,
    string? ScheduleTimeZone = null);

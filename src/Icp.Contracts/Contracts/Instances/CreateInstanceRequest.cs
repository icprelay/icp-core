namespace Icp.Contracts.Instances;

public sealed record CreateInstanceRequest(
    string IntegrationTarget,
    string SubscribedEventType,
    bool Enabled,
    string IntegrationTargetParametersJson,
    string EventParametersJson,
    string SecretRefsJson,
    string CustomerName,
    Guid? AccountId = null,
    string TriggerType = "Event",
    string? ScheduleCron = null,
    string? ScheduleTimeZone = null);

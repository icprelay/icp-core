namespace Icp.Contracts.EventTraces;

public sealed record CreateEventStepRequest(
    Guid? Id,
    string StepName,
    string Status,
    Guid? ExecutionId,
    Guid? InstanceId,
    string? TargetType,
    string? LogicAppRunId,
    string? Message);

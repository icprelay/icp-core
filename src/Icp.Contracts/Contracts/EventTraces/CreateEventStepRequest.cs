namespace Icp.Contracts.EventTraces;

public sealed record CreateEventStepRequest(
    Guid? Id,
    string StepName,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    Guid? ExecutionId,
    Guid? InstanceId,
    string? TargetType,
    string? LogicAppRunId,
    string? Message);

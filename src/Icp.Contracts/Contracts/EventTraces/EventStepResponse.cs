namespace Icp.Contracts.EventTraces;

public sealed record EventStepResponse(
    Guid Id,
    Guid EventId,
    string StepName,
    string Status,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    Guid? ExecutionId,
    Guid? InstanceId,
    string? TargetType,
    string? LogicAppRunId,
    string? Message);

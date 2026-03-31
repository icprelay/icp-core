namespace Icp.Contracts.EventTraces;

public sealed record EventStepResponse(
    Guid Id,
    Guid EventId,
    string StepName,
    string Status,
    DateTime TimestampUtc,
    Guid? ExecutionId,
    Guid? InstanceId,
    string? TargetType,
    string? LogicAppRunId,
    string? Message);

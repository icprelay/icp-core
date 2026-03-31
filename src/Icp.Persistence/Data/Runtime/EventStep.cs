namespace Icp.Persistence.Data.Runtime;

/// <summary>
/// Detailed timeline. Represents a single step within an event workflow, including its identifiers, status, and timing information.
/// </summary>
/// <remarks>Use this class to track the progress and details of individual steps in a multi-step event processing
/// scenario. Each instance contains information about the step's identity, execution context, and lifecycle timestamps.
/// This type is typically used in event orchestration or workflow tracking systems.</remarks>
public class EventStep
{
    public required Guid Id { get; set; }
    public required Guid EventId { get; set; }
    public required string StepName { get; set; }
    public required string Status { get; set; }
    public required DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Guid? ExecutionId { get; set; }
    public Guid? InstanceId { get; set; }
    public string? LogicAppRunId { get; set; }
    public string? Message { get; set; }
    public string? TargetType { get; set; }

    public EventTrace? EventTrace { get; set; }
    public Run? Execution { get; set; }
    public IntegrationInstance? Instance { get; set; }
}

public static class EventSteps
{
    public const string IngestReceived = "ingest.received";
    public const string MapperStarted = "mapper.started";
    public const string MapperCompleted = "mapper.completed";
    public const string DispatchStarted = "dispatch.started";
    public const string DispatchCompleted = "dispatch.completed";
    public const string TargetStarted = "target.started";
    public const string TargetCompleted = "target.completed";
    public const string TargetFailed = "target.failed";
}
namespace Icp.Persistence.Data.Runtime;
/// <summary>
/// Event lifecycle overview. Represents the trace information for a processed event, including identifiers, status, timestamps, and processing
/// statistics.
/// </summary>
/// <remarks>Use this class to track the lifecycle and processing details of an event within the system. It
/// provides correlation and status information that can be used for diagnostics, auditing, or monitoring event
/// processing workflows.</remarks>
public class EventTrace
{
    public required Guid EventId { get; set; }
    public string? CorrelationId { get; set; }
    public required string AccountKey { get; set; }
    public required string EventType { get; set; }
    public required string Status { get; set; }
    public required string CurrentStage { get; set; }
    public required DateTime ReceivedAtUtc { get; set; }
    public DateTime LastUpdatedAtUtc { get; set; }
    public int MatchedInstanceCount { get; set; } = 0;
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string BlobRef { get; set; } = string.Empty;
}

public static class EventStages
{
    public const string Ingest = "ingest";
    public const string Mapper = "mapper";
    public const string Dispatch = "dispatch";
    public const string Target = "target";
    public const string Completed = "completed";
}

public static class EventStatus
{
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string PartialSuccess = "partial_success";
}
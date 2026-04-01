namespace Icp.Contracts.EventTraces;

public sealed record EventTraceResponse(
    Guid EventId,
    string? CorrelationId,
    string AccountKey,
    string EventType,
    string Status,
    string CurrentStage,
    DateTime ReceivedAtUtc,
    DateTime LastUpdatedAtUtc,
    int MatchedInstanceCount,
    int SuccessCount,
    int FailureCount,
    string BlobRef);

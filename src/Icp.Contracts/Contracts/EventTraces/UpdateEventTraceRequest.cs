namespace Icp.Contracts.EventTraces;

public sealed record UpdateEventTraceRequest(
    string? Status,
    string? CurrentStage,
    string? EventType,
    int? MatchedInstanceCount,
    int? SuccessCount,
    int? FailureCount,
    string? BlobRef);

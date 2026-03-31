namespace Icp.Contracts.EventTraces;

public sealed record UpdateEventTraceRequest(
    string? Status,
    string? CurrentStage,
    int? MatchedInstanceCount,
    int? SuccessCount,
    int? FailureCount,
    string? BlobRef);

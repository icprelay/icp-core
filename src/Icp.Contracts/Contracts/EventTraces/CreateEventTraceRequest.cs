namespace Icp.Contracts.EventTraces;

public sealed record CreateEventTraceRequest(
    Guid EventId,
    string? CorrelationId,
    string AccountKey,
    string EventType,
    DateTime ReceivedAtUtc);

namespace Icp.Contracts.Runs;

public sealed record UpdateRunRequest(
    string? Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    string? Error,
    string? OutputFullBlobPath,
    string? OutputContentType);

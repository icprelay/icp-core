namespace Icp.Storage.Models;

public sealed record StoredBlob(
    string Container,
    string Name,
    Uri Uri,
    string ContentType,
    long? SizeBytes,
    string? ETag,
    DateTimeOffset? CreatedAt
);

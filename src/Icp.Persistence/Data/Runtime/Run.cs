using System.ComponentModel.DataAnnotations;

namespace Icp.Persistence.Data.Runtime;

public class Run
{
    [Key]
    public Guid RunId { get; set; }

    [Required]
    public Guid InstanceId { get; set; }

    public IntegrationInstance? Instance { get; set; }

    [Required]
    public string CorrelationId { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "queued";

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public string? Error { get; set; }

    public string? OutputFullBlobPath { get; set; }

    public string? OutputContentType { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

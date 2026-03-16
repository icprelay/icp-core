using System.ComponentModel.DataAnnotations;

namespace Icp.Persistence.Data.Runtime;

public class IntegrationInstance
{
    [Key]
    public Guid InstanceId { get; set; }

    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public string IntegrationTarget { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    [Required]
    public string IntegrationTargetParametersJson { get; set; } = "{}";

    [Required]
    public string EventParametersJson { get; set; } = "{}";

    [Required]
    public string SecretRefsJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<Run> Runs { get; set; } = new List<Run>();

    [Required]
    public string SubscribedEventType { get; set; } = string.Empty;

    public EventType? EventType { get; set; }

    public IntegrationTarget? Target { get; set; }

    public IntegrationAccount? Account { get; set; }

    [Required]
    [MaxLength(20)]
    public string TriggerType { get; set; } = "Event";

    [MaxLength(100)]
    public string? ScheduleCron { get; set; }

    [MaxLength(100)]
    public string? ScheduleTimeZone { get; set; }

    public DateTimeOffset? NextDueAtUtc { get; set; }

    public int ScheduleVersion { get; set; } = 1;

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;

namespace Icp.Persistence.Data.Runtime;

public class IntegrationAccount
{
    [Key]
    public Guid AccountId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string ExternalCustomerId { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string InboundKeyHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<IntegrationInstance> IntegrationInstances { get; set; } = new List<IntegrationInstance>();
}

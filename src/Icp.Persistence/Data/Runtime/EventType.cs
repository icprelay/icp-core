using System.ComponentModel.DataAnnotations;

namespace Icp.Persistence.Data.Runtime;

public class EventType
{
    [Key]
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string AllowedTriggerTypes { get; set; } = "Event";

    [Required]
    public string ParametersTemplateJson { get; set; } = "{}";

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string IconKey { get; set; } = string.Empty;

    public ICollection<IntegrationInstance> IntegrationInstances { get; set; } = new List<IntegrationInstance>();
}

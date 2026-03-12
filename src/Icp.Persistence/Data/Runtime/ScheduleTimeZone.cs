using System.ComponentModel.DataAnnotations;

namespace Icp.Persistence.Data.Runtime;

public class ScheduleTimeZone
{
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public int SortOrder { get; set; }
}

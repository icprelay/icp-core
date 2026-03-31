namespace Icp.Ui.ViewModels;

public sealed class EventTraceViewModel
{
    public Guid EventId { get; init; }
    public DateTime ReceivedAtUtc { get; init; }
    public string AccountKey { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string CurrentStage { get; init; } = string.Empty;
    public IReadOnlyList<string> TargetStatuses { get; init; } = [];
}

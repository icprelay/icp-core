using Icp.Contracts.Instances;
using Icp.Contracts.Runs;

namespace Icp.Ui;

public sealed record InstanceListItemViewModel(
    InstanceResponse Instance,
    RunResponse? LastRun,
    string EventTypeDisplayName,
    string EventTypeIconKey,
    string IntegrationTargetDisplayName,
    string IntegrationTargetIconKey,
    bool RequiresSecrets);

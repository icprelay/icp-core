using Icp.Contracts.Instances;

namespace Icp.Contracts.Scheduler;

public sealed record SchedulerTickResponse(IReadOnlyList<InstanceResponse> DueInstances);

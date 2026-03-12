using Icp.Contracts.Instances;
using Icp.Contracts.Scheduler;
using Icp.Persistence.Data.Runtime;
using Microsoft.EntityFrameworkCore;
using Icp.Api.Host.Scheduling;

namespace Icp.Api.Host.Endpoints;

public static class SchedulerEndpoints
{
    public static IEndpointRouteBuilder MapSchedulerEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            var grp = app.MapGroup("/api")
                .RequireAuthorization("Worker")
                .WithTags("Scheduler");

            grp.MapPost("/scheduler/tick", Tick)
                .WithName("SchedulerTick");

            return app;
        }
        else
        {
            var grp = app.MapGroup("/api")
                .WithTags("Scheduler");

            grp.MapPost("/scheduler/tick", Tick)
                .WithName("SchedulerTick");

            return app;
        }
    }

    private static async Task<IResult> Tick(SchedulerTickRequest request, RuntimeDbContext db, CancellationToken ct)
    {
        if (request is null)
            return Results.BadRequest("payload is required");

        if (request.LookAheadSeconds < 0)
            return Results.BadRequest("lookAheadSeconds must be >= 0");

        var now = DateTimeOffset.UtcNow;
        var max = now.AddSeconds(request.LookAheadSeconds);
        var min = now.AddHours(-2); // policy: max 2h catch-up

        // Load tracked entities so we can advance NextDueAtUtc.
        var dueEntities = await db.IntegrationInstances
            .Where(x => x.Enabled)
            .Where(x => x.TriggerType == "Schedule")
            .Where(x => x.NextDueAtUtc != null && x.NextDueAtUtc >= min && x.NextDueAtUtc <= max)
            .OrderBy(x => x.NextDueAtUtc)
            .ToListAsync(ct);

        foreach (var entity in dueEntities)
        {
            // Advance using the previous due time as the reference to avoid drifting/skipping.
            var reference = entity.NextDueAtUtc ?? now;
            entity.NextDueAtUtc = ScheduleDueCalculator.ComputeNextDueAtUtc(entity.ScheduleCron, entity.ScheduleTimeZone, reference)
                                 ?? ScheduleDueCalculator.ComputeNextDueAtUtc(entity.ScheduleCron, entity.ScheduleTimeZone, now);
            entity.UpdatedAt = now;
        }

        if (dueEntities.Count > 0)
            await db.SaveChangesAsync(ct);

        var due = dueEntities
            .Select(x => new InstanceResponse(
                x.InstanceId,
                x.CustomerId,
                x.CustomerName,
                x.IntegrationTarget,
                x.SubscribedEventType,
                x.Enabled,
                string.IsNullOrWhiteSpace(x.DisplayName) ? x.IntegrationTarget : x.DisplayName,
                x.IntegrationTargetParametersJson,
                x.EventParametersJson,
                x.SecretRefsJson,
                x.CreatedAt,
                x.UpdatedAt,
                x.TriggerType,
                x.ScheduleCron,
                x.ScheduleTimeZone,
                x.NextDueAtUtc,
                x.ScheduleVersion))
            .ToList();

        return Results.Ok(new SchedulerTickResponse(due));
    }
}

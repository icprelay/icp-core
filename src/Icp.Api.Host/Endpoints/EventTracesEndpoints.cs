using Icp.Contracts.EventTraces;
using Icp.Persistence.Data.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Icp.Api.Host.Endpoints;

public static class EventTracesEndpoints
{
    public static IEndpointRouteBuilder MapEventTracesEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            #region production
            var prod_grp = app.MapGroup("/api")
                .WithTags("EventTraces");

            // ui methods - read
            prod_grp.MapGet("/event-traces", ListEventTraces)
                .RequireAuthorization("UI")
                .WithName("ListEventTraces");

            prod_grp.MapGet("/event-traces/{eventId:guid}", GetEventTrace)
                .RequireAuthorization("UI")
                .WithName("GetEventTrace");

            prod_grp.MapGet("/event-traces/{eventId:guid}/steps", ListEventSteps)
                .RequireAuthorization("UI")
                .WithName("ListEventSteps");

            // worker methods - write
            prod_grp.MapPost("/event-traces", CreateEventTrace)
                .RequireAuthorization("Worker")
                .WithName("CreateEventTrace");

            prod_grp.MapPatch("/event-traces/{eventId:guid}", UpdateEventTrace)
                .RequireAuthorization("Worker")
                .WithName("UpdateEventTrace");

            prod_grp.MapPost("/event-traces/{eventId:guid}/steps", CreateEventStep)
                .RequireAuthorization("Worker")
                .WithName("CreateEventStep");

            return app;
            #endregion
        }
        else
        {
            #region development
            var dev_grp = app.MapGroup("/api")
                .WithTags("EventTraces");

            dev_grp.MapGet("/event-traces", ListEventTraces)
                .WithName("ListEventTraces");

            dev_grp.MapGet("/event-traces/{eventId:guid}", GetEventTrace)
                .WithName("GetEventTrace");

            dev_grp.MapGet("/event-traces/{eventId:guid}/steps", ListEventSteps)
                .WithName("ListEventSteps");

            dev_grp.MapPost("/event-traces", CreateEventTrace)
                .WithName("CreateEventTrace");

            dev_grp.MapPatch("/event-traces/{eventId:guid}", UpdateEventTrace)
                .WithName("UpdateEventTrace");

            dev_grp.MapPost("/event-traces/{eventId:guid}/steps", CreateEventStep)
                .WithName("CreateEventStep");

            return app;
            #endregion
        }
    }

    private static async Task<IResult> ListEventTraces(
        string? accountKey,
        string? status,
        int? limit,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        IQueryable<EventTrace> query = db.EventTraces;

        if (!string.IsNullOrWhiteSpace(accountKey))
            query = query.Where(x => x.AccountKey == accountKey);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        var take = Math.Clamp(limit ?? 50, 1, 200);

        var traces = await query
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(take)
            .ToListAsync(ct);

        return Results.Ok(traces.Select(ToResponse));
    }

    private static async Task<IResult> GetEventTrace(
        Guid eventId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        var trace = await db.EventTraces.SingleOrDefaultAsync(x => x.EventId == eventId, ct);
        if (trace is null)
            return Results.NotFound();

        return Results.Ok(ToResponse(trace));
    }

    private static async Task<IResult> ListEventSteps(
        Guid eventId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        var traceExists = await db.EventTraces.AnyAsync(x => x.EventId == eventId, ct);
        if (!traceExists)
            return Results.NotFound($"EventTrace with eventId '{eventId}' not found");

        var steps = await db.EventSteps
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.TimestampUtc)
            .ToListAsync(ct);

        return Results.Ok(steps.Select(ToStepResponse));
    }

    private static async Task<IResult> CreateEventTrace(
        CreateEventTraceRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (request.EventId == Guid.Empty)
            return Results.BadRequest("eventId is required");
        if (string.IsNullOrWhiteSpace(request.AccountKey))
            return Results.BadRequest("accountKey is required");
        if (string.IsNullOrWhiteSpace(request.EventType))
            return Results.BadRequest("eventType is required");

        var exists = await db.EventTraces.AnyAsync(x => x.EventId == request.EventId, ct);
        if (exists)
            return Results.Conflict($"EventTrace with eventId '{request.EventId}' already exists");

        var trace = new EventTrace
        {
            EventId = request.EventId,
            CorrelationId = request.CorrelationId,
            AccountKey = request.AccountKey,
            EventType = request.EventType,
            ReceivedAtUtc = request.ReceivedAtUtc,
            Status = EventStatus.Running,
            CurrentStage = EventStages.Ingest,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        db.EventTraces.Add(trace);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/event-traces/{trace.EventId}", ToResponse(trace));
    }

    private static async Task<IResult> UpdateEventTrace(
        Guid eventId,
        UpdateEventTraceRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !ValidStatuses.Contains(request.Status))
            return Results.BadRequest($"Invalid status '{request.Status}'. Valid values: {string.Join(", ", ValidStatuses)}");

        if (!string.IsNullOrWhiteSpace(request.CurrentStage) && !ValidStages.Contains(request.CurrentStage))
            return Results.BadRequest($"Invalid currentStage '{request.CurrentStage}'. Valid values: {string.Join(", ", ValidStages)}");

        var trace = await db.EventTraces.SingleOrDefaultAsync(x => x.EventId == eventId, ct);
        if (trace is null)
            return Results.NotFound();

        // Update stage if explicitly provided (for early stages: ingest, mapper, dispatch)
        if (!string.IsNullOrWhiteSpace(request.CurrentStage))
            trace.CurrentStage = request.CurrentStage;
        
        if (!string.IsNullOrWhiteSpace(request.EventType))
            trace.EventType = request.EventType;

        if (request.MatchedInstanceCount.HasValue)
            trace.MatchedInstanceCount = request.MatchedInstanceCount.Value;

        // Increment counts (append to existing values)
        if (request.SuccessCount.HasValue)
            trace.SuccessCount += request.SuccessCount.Value;

        if (request.FailureCount.HasValue)
            trace.FailureCount += request.FailureCount.Value;

        if (!string.IsNullOrWhiteSpace(request.BlobRef))
            trace.BlobRef = request.BlobRef;

        // Auto-compute final status when all targets complete
        var totalCompleted = trace.SuccessCount + trace.FailureCount;
        if (trace.MatchedInstanceCount > 0 && totalCompleted >= trace.MatchedInstanceCount)
        {
            // All targets done - auto-set stage and compute status
            trace.CurrentStage = EventStages.Completed;

            if (trace.FailureCount == 0)
                trace.Status = EventStatus.Succeeded;
            else if (trace.SuccessCount == 0)
                trace.Status = EventStatus.Failed;
            else
                trace.Status = EventStatus.PartialSuccess;
        }
        else if (!string.IsNullOrWhiteSpace(request.Status))
        {
            // Allow explicit status for non-target stages (ingest, mapper, dispatch failures)
            trace.Status = request.Status;
        }

        trace.LastUpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(trace));
    }

    private static async Task<IResult> CreateEventStep(
        Guid eventId,
        CreateEventStepRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.StepName))
            return Results.BadRequest("stepName is required");
        if (string.IsNullOrWhiteSpace(request.Status))
            return Results.BadRequest("status is required");

        if (!ValidStepNames.Contains(request.StepName))
            return Results.BadRequest($"Invalid stepName '{request.StepName}'. Valid values: {string.Join(", ", ValidStepNames)}");

        if (!ValidStatuses.Contains(request.Status))
            return Results.BadRequest($"Invalid status '{request.Status}'. Valid values: {string.Join(", ", ValidStatuses)}");

        var traceExists = await db.EventTraces.AnyAsync(x => x.EventId == eventId, ct);
        if (!traceExists)
            return Results.NotFound($"EventTrace with eventId '{eventId}' not found");

        // If ID provided, check for idempotency (conflict on duplicate)
        var stepId = request.Id ?? Guid.NewGuid();
        if (request.Id.HasValue)
        {
            var stepExists = await db.EventSteps.AnyAsync(x => x.Id == request.Id.Value, ct);
            if (stepExists)
                return Results.Conflict($"EventStep with id '{request.Id}' already exists");
        }

        var step = new EventStep
        {
            Id = stepId,
            EventId = eventId,
            StepName = request.StepName,
            Status = request.Status,
            TimestampUtc = DateTime.UtcNow,
            ExecutionId = request.ExecutionId,
            InstanceId = request.InstanceId,
            TargetType = request.TargetType,
            LogicAppRunId = request.LogicAppRunId,
            Message = request.Message
        };

        db.EventSteps.Add(step);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/event-traces/{eventId}/steps/{step.Id}", ToStepResponse(step));
    }

    private static EventTraceResponse ToResponse(EventTrace trace) =>
        new(
            trace.EventId,
            trace.CorrelationId,
            trace.AccountKey,
            trace.EventType,
            trace.Status,
            trace.CurrentStage,
            trace.ReceivedAtUtc,
            trace.LastUpdatedAtUtc,
            trace.MatchedInstanceCount,
            trace.SuccessCount,
            trace.FailureCount,
            trace.BlobRef);

    private static EventStepResponse ToStepResponse(EventStep step) =>
        new(
            step.Id,
            step.EventId,
            step.StepName,
            step.Status,
            step.TimestampUtc,
            step.ExecutionId,
            step.InstanceId,
            step.TargetType,
            step.LogicAppRunId,
            step.Message);


    private static readonly HashSet<string> ValidStatuses =
    [
        EventStatus.Running,
        EventStatus.Succeeded,
        EventStatus.Failed,
        EventStatus.PartialSuccess
    ];

    private static readonly HashSet<string> ValidStages =
    [
        EventStages.Ingest,
        EventStages.Mapper,
        EventStages.Dispatch,
        EventStages.Target,
        EventStages.Completed
    ];

    private static readonly HashSet<string> ValidStepNames =
    [
        EventSteps.IngestReceived,
        EventSteps.IngestCompleted,
        EventSteps.MapperStarted,
        EventSteps.MapperCompleted,
        EventSteps.DispatchStarted,
        EventSteps.DispatchCompleted,
        EventSteps.TargetStarted,
        EventSteps.TargetCompleted,
        EventSteps.TargetFailed
    ];
}

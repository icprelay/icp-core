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
                .RequireAuthorization("Worker")
                .WithTags("EventTraces");

            prod_grp.MapPost("/event-traces", CreateEventTrace)
                .WithName("CreateEventTrace");

            prod_grp.MapPatch("/event-traces/{eventId:guid}", UpdateEventTrace)
                .WithName("UpdateEventTrace");

            prod_grp.MapPost("/event-traces/{eventId:guid}/steps", CreateEventStep)
                .WithName("CreateEventStep");

            return app;
            #endregion
        }
        else
        {
            #region development
            var dev_grp = app.MapGroup("/api")
                .WithTags("EventTraces");

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

        if (!string.IsNullOrWhiteSpace(request.Status))
            trace.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.CurrentStage))
            trace.CurrentStage = request.CurrentStage;

        if (request.MatchedInstanceCount.HasValue)
            trace.MatchedInstanceCount = request.MatchedInstanceCount.Value;

        if (request.SuccessCount.HasValue)
            trace.SuccessCount = request.SuccessCount.Value;

        if (request.FailureCount.HasValue)
            trace.FailureCount = request.FailureCount.Value;

        if (!string.IsNullOrWhiteSpace(request.BlobRef))
            trace.BlobRef = request.BlobRef;

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
        if (request.Id == Guid.Empty)
            return Results.BadRequest("id is required");
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

        var stepExists = await db.EventSteps.AnyAsync(x => x.Id == request.Id, ct);
        if (stepExists)
            return Results.Conflict($"EventStep with id '{request.Id}' already exists");

        var step = new EventStep
        {
            Id = request.Id,
            EventId = eventId,
            StepName = request.StepName,
            Status = request.Status,
            StartedAtUtc = request.StartedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
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
            step.StartedAtUtc,
            step.CompletedAtUtc,
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
        EventSteps.MapperStarted,
        EventSteps.MapperCompleted,
        EventSteps.DispatchStarted,
        EventSteps.DispatchCompleted,
        EventSteps.TargetStarted,
        EventSteps.TargetCompleted,
        EventSteps.TargetFailed
    ];
}

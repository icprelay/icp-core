using Azure.Messaging.ServiceBus;
using Icp.Api.Host.Scheduling;
using Icp.Contracts.Common;
using Icp.Contracts.Contracts.Messaging;
using Icp.Contracts.Instances;
using Icp.Contracts.Runs;
using Icp.Messaging.ServiceBus;
using Icp.Persistence.Data.Runtime;
using Icp.Secrets;
using Icp.Storage;
using Icp.Storage.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Icp.Api.Host.Endpoints;

public static class InstancesEndpoints
{
    public static IEndpointRouteBuilder MapInstanceEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            #region production
            var prod_grp = app.MapGroup("/api")
                .WithTags("Instances");

            prod_grp.MapGet("/instances/{instanceId}", GetInstance)
                .RequireAuthorization("WorkerOrUI")
                .WithName("GetInstance");

            prod_grp.MapPost("/customers/{customerId}/instances", CreateInstance)
                .RequireAuthorization("UI")
                .WithName("CreateInstance");

            prod_grp.MapPut("/instances/{instanceId:guid}", UpdateInstance)
                .RequireAuthorization("UI")
                .WithName("UpdateInstance");

            prod_grp.MapPost("/instances/{instanceId:guid}/enable", EnableInstance)
                .RequireAuthorization("UI")
                .WithName("EnableInstance");

            prod_grp.MapPost("/instances/{instanceId:guid}/disable", DisableInstance)
                .RequireAuthorization("UI")
                .WithName("DisableInstance");

            prod_grp.MapGet("/customers/{customerId}/instances", ListInstances)
                .RequireAuthorization("UI")
                .WithName("ListInstances");

            prod_grp.MapGet("/instances", ListAllInstances)
                .RequireAuthorization("WorkerOrUI")
                .WithName("ListAllInstances");

            prod_grp.MapGet("/instances/humans", ListAllHumanInstances)
                .RequireAuthorization("UI")
                .WithName("ListAllHumanInstances");

            prod_grp.MapPost("/instances/{instanceId:guid}/secrets", SetInstanceSecrets)
                .RequireAuthorization("UI")
                .WithName("SetInstanceSecrets");

            prod_grp.MapPost("/instances/{instanceId:guid}/run", StartRun)
                .RequireAuthorization("Worker")
                .WithName("StartRun");

            prod_grp.MapPost("/instances/{instanceId:guid}/invoke", ManualInvoke)
                .RequireAuthorization("UI")
                .WithName("ManualInvoke");

            prod_grp.MapPut("/runs/{runId:guid}", UpdateRun)
                .RequireAuthorization("Worker")
                .WithName("UpdateRun");

            prod_grp.MapGet("/runs/{runId:guid}/output", GetRunOutput)
                .RequireAuthorization("UI")
                .WithName("GetRunOutput");

            prod_grp.MapGet("/instances/{instanceId:guid}/runs", ListRuns)
                .RequireAuthorization("UI")
                .WithName("ListRuns");

            prod_grp.MapDelete("/instances/{instanceId:guid}", DeleteInstance)
                .RequireAuthorization("UI")
                .WithName("DeleteInstance");

            prod_grp.MapGet("/runs", ListAllRuns)
                .RequireAuthorization("UI")
                .WithName("ListAllRuns");

            return app;
            #endregion
        }
        else
        {
            #region development
            var dev_grp = app.MapGroup("/api")
                .WithTags("Instances");

            dev_grp.MapGet("/instances/{instanceId}", GetInstance)
                .WithName("GetInstance");

            dev_grp.MapPost("/customers/{customerId}/instances", CreateInstance)
                .WithName("CreateInstance");

            dev_grp.MapPut("/instances/{instanceId:guid}", UpdateInstance)
                .WithName("UpdateInstance");

            dev_grp.MapPost("/instances/{instanceId:guid}/enable", EnableInstance)
                .WithName("EnableInstance");

            dev_grp.MapPost("/instances/{instanceId:guid}/disable", DisableInstance)
                .WithName("DisableInstance");

            dev_grp.MapGet("/customers/{customerId}/instances", ListInstances)
                .WithName("ListInstances");

            dev_grp.MapGet("/instances", ListAllInstances)
                .WithName("ListAllInstances");

            dev_grp.MapGet("/instances/humans", ListAllHumanInstances)
                .WithName("ListAllHumanInstances");

            dev_grp.MapPost("/instances/{instanceId:guid}/secrets", SetInstanceSecrets)
                .WithName("SetInstanceSecrets");

            dev_grp.MapPost("/instances/{instanceId:guid}/run", StartRun)
                .WithName("StartRun");

            dev_grp.MapPost("/instances/{instanceId:guid}/invoke", ManualInvoke)
                .WithName("ManualInvoke");

            dev_grp.MapPut("/runs/{runId:guid}", UpdateRun)
                .WithName("UpdateRun");

            dev_grp.MapGet("/runs/{runId:guid}/output", GetRunOutput)
                .WithName("GetRunOutput");

            dev_grp.MapGet("/instances/{instanceId:guid}/runs", ListRuns)
                .WithName("ListRuns");

            dev_grp.MapDelete("/instances/{instanceId:guid}", DeleteInstance)
                .WithName("DeleteInstance");

            dev_grp.MapGet("/runs", ListAllRuns)
                .WithName("ListAllRuns");

            return app;
            #endregion
        }
    }

    private static async Task<IResult> GetRunOutput(
        string runId,
        RuntimeDbContext db,
        IBlobStorageService storage,
        Microsoft.Extensions.Options.IOptions<StorageOptions> storageOptions,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(runId))
            return Results.BadRequest("runId is required");

        var run = await db.Runs.SingleOrDefaultAsync(x => x.RunId.ToString() == runId, ct);

        if (run?.OutputFullBlobPath is null)
            return Results.NotFound("output not found for the run");

        var output = await storage.DownloadAsync(run.OutputFullBlobPath, ct);

        return Results.File(output, run.OutputContentType ?? "application/octet-stream");
    }

    private static async Task<IResult> GetInstance(string instanceId, RuntimeDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return Results.BadRequest("instanceId is required");

        var instance = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId.ToString() == instanceId, ct);

        if (instance == null)
            return Results.NotFound();

        return Results.Ok(ToResponse(instance));
    }

    private static async Task<IResult> CreateInstance(
        string customerId,
        CreateInstanceRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Results.BadRequest("customerId is required");
        if (string.IsNullOrWhiteSpace(request.IntegrationTarget))
            return Results.BadRequest("integrationTarget is required");
        if (string.IsNullOrWhiteSpace(request.SubscribedEventType))
            return Results.BadRequest("subscribedEventType is required");
        if (string.IsNullOrWhiteSpace(request.IntegrationTargetParametersJson))
            return Results.BadRequest("integrationTargetParametersJson is required");
        if (string.IsNullOrWhiteSpace(request.EventParametersJson))
            return Results.BadRequest("eventParametersJson is required");
        if (string.IsNullOrWhiteSpace(request.SecretRefsJson))
            return Results.BadRequest("secretRefsJson is required");

        var triggerType = (request.TriggerType ?? "Event").Trim();
        if (!triggerType.Equals("Event", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("triggerType must be 'Event', 'Schedule' or 'OnDemand'");
        }

        var eventTypeName = request.SubscribedEventType.Trim();
        var eventType = await db.EventTypes.SingleOrDefaultAsync(x => x.Name == eventTypeName, ct);
        if (eventType is null)
        {
            // Maintain previous behavior: auto-create.
            eventType = new EventType { Name = eventTypeName };
            db.EventTypes.Add(eventType);
        }

        if (!eventType.AllowedTriggerTypes.Contains(triggerType, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest($"subscribedEventType is not allowed for triggerType '{triggerType}'");

        if (triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ScheduleCron))
                return Results.BadRequest("scheduleCron is required when triggerType is 'Schedule'");
            if (string.IsNullOrWhiteSpace(request.ScheduleTimeZone))
                return Results.BadRequest("scheduleTimeZone is required when triggerType is 'Schedule'");
            if (!await IsAllowedScheduleTimeZoneAsync(db, request.ScheduleTimeZone, ct))
                return Results.BadRequest("scheduleTimeZone is invalid");
        }
        else
        {
            // OnDemand/Event triggers must not include schedule configuration.
            if (!string.IsNullOrWhiteSpace(request.ScheduleCron) || !string.IsNullOrWhiteSpace(request.ScheduleTimeZone))
                return Results.BadRequest("scheduleCron/scheduleTimeZone are only allowed for triggerType 'Schedule'");
        }

        var targetName = request.IntegrationTarget.Trim();

        var target = await db.IntegrationTargets.SingleOrDefaultAsync(x => x.Name == targetName, ct);
        if (target is null)
        {
            // Maintain previous behavior: auto-create.
            target = new IntegrationTarget { Name = targetName, ParametersTemplateJson = "{}" };
            db.IntegrationTargets.Add(target);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(target.AllowedTriggerTypes) &&
                !target.AllowedTriggerTypes.Contains(triggerType, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest($"integrationTarget is not allowed for triggerType '{triggerType}'");
            }
        }

        var now = DateTimeOffset.UtcNow;

        var scheduleCron = string.IsNullOrWhiteSpace(request.ScheduleCron) ? null : request.ScheduleCron.Trim();
        var scheduleTz = string.IsNullOrWhiteSpace(request.ScheduleTimeZone) ? null : request.ScheduleTimeZone.Trim();

        var nextDueAtUtc = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            ? ScheduleDueCalculator.ComputeNextDueAtUtc(scheduleCron, scheduleTz, now)
            : null;

        var entity = new IntegrationInstance
        {
            InstanceId = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? customerId : request.CustomerName.Trim(),
            IntegrationTarget = targetName,
            DisplayName = targetName,
            SubscribedEventType = eventTypeName,
            Enabled = request.Enabled,
            IntegrationTargetParametersJson = request.IntegrationTargetParametersJson,
            EventParametersJson = request.EventParametersJson,
            SecretRefsJson = request.SecretRefsJson,
            TriggerType = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                ? "Schedule"
                : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                    ? "OnDemand"
                    : "Event",
            ScheduleCron = scheduleCron,
            ScheduleTimeZone = scheduleTz,
            NextDueAtUtc = nextDueAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.IntegrationInstances.Add(entity);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/instances/{entity.InstanceId}", ToResponse(entity));
    }

    private static async Task<IResult> UpdateInstance(
        Guid instanceId,
        UpdateInstanceRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");
        if (string.IsNullOrWhiteSpace(request.IntegrationTarget))
            return Results.BadRequest("integrationTarget is required");
        if (string.IsNullOrWhiteSpace(request.SubscribedEventType))
            return Results.BadRequest("subscribedEventType is required");
        if (string.IsNullOrWhiteSpace(request.IntegrationTargetParametersJson))
            return Results.BadRequest("integrationTargetParametersJson is required");
        if (string.IsNullOrWhiteSpace(request.EventParametersJson))
            return Results.BadRequest("eventParametersJson is required");
        if (string.IsNullOrWhiteSpace(request.SecretRefsJson))
            return Results.BadRequest("secretRefsJson is required");

        var entity = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (entity is null)
            return Results.NotFound();

        var triggerType = (request.TriggerType ?? "Event").Trim();
        if (!triggerType.Equals("Event", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("triggerType must be 'Event', 'Schedule' or 'OnDemand'");
        }

        var eventTypeName = request.SubscribedEventType.Trim();
        var eventType = await db.EventTypes.SingleOrDefaultAsync(x => x.Name == eventTypeName, ct);
        if (eventType is null)
        {
            // Maintain previous behavior: auto-create.
            eventType = new EventType { Name = eventTypeName };
            db.EventTypes.Add(eventType);
        }

        if (!eventType.AllowedTriggerTypes.Contains(triggerType, StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest($"subscribedEventType is not allowed for triggerType '{triggerType}'");

        if (triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(request.ScheduleCron))
                return Results.BadRequest("scheduleCron is required when triggerType is 'Schedule'");
            if (string.IsNullOrWhiteSpace(request.ScheduleTimeZone))
                return Results.BadRequest("scheduleTimeZone is required when triggerType is 'Schedule'");
            if (!await IsAllowedScheduleTimeZoneAsync(db, request.ScheduleTimeZone, ct))
                return Results.BadRequest("scheduleTimeZone is invalid");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.ScheduleCron) || !string.IsNullOrWhiteSpace(request.ScheduleTimeZone))
                return Results.BadRequest("scheduleCron/scheduleTimeZone are only allowed for triggerType 'Schedule'");
        }

        var targetName = request.IntegrationTarget.Trim();

        var target = await db.IntegrationTargets.SingleOrDefaultAsync(x => x.Name == targetName, ct);
        if (target is null)
        {
            // Maintain previous behavior: auto-create.
            target = new IntegrationTarget { Name = targetName, ParametersTemplateJson = "{}" };
            db.IntegrationTargets.Add(target);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(target.AllowedTriggerTypes) &&
                !target.AllowedTriggerTypes.Contains(triggerType, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest($"integrationTarget is not allowed for triggerType '{triggerType}'");
            }
        }

        var now = DateTimeOffset.UtcNow;

        entity.IntegrationTarget = targetName;
        entity.SubscribedEventType = eventTypeName;
        entity.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? targetName : request.DisplayName.Trim();
        entity.IntegrationTargetParametersJson = request.IntegrationTargetParametersJson;
        entity.EventParametersJson = request.EventParametersJson;
        entity.SecretRefsJson = request.SecretRefsJson;
        entity.TriggerType = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            ? "Schedule"
            : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                ? "OnDemand"
                : "Event";
        entity.ScheduleCron = string.IsNullOrWhiteSpace(request.ScheduleCron) ? null : request.ScheduleCron.Trim();
        entity.ScheduleTimeZone = string.IsNullOrWhiteSpace(request.ScheduleTimeZone) ? null : request.ScheduleTimeZone.Trim();
        entity.NextDueAtUtc = entity.TriggerType == "Schedule"
            ? ScheduleDueCalculator.ComputeNextDueAtUtc(entity.ScheduleCron, entity.ScheduleTimeZone, now)
            : null;
        entity.ScheduleVersion += 1;
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(entity));
    }

    private static async Task<IResult> EnableInstance(
        Guid instanceId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        var entity = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (entity is null)
            return Results.NotFound();

        if (!entity.Enabled)
        {
            entity.Enabled = true;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ToResponse(entity));
    }

    private static async Task<IResult> DisableInstance(
        Guid instanceId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        var entity = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (entity is null)
            return Results.NotFound();

        if (entity.Enabled)
        {
            entity.Enabled = false;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ToResponse(entity));
    }

    private static async Task<IResult> ListInstances(
        string customerId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Results.BadRequest("customerId is required");


        var items = await db.IntegrationInstances
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.CreatedAt)
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
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> ListAllInstances(
        string? subscribedEventType,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        IQueryable<IntegrationInstance> query = db.IntegrationInstances;

        if (!string.IsNullOrWhiteSpace(subscribedEventType))
        {
            var evt = subscribedEventType.Trim();
            query = query.Where(x => x.SubscribedEventType == evt);
        }

        var items = await query
            .OrderBy(x => x.CreatedAt)
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
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> ListAllHumanInstances(
        string? subscribedEventType,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        IQueryable<IntegrationInstance> query = db.IntegrationInstances
            .Where(x => x.CustomerId != Guid.Empty.ToString("D"));

        if (!string.IsNullOrWhiteSpace(subscribedEventType))
        {
            var evt = subscribedEventType.Trim();
            query = query.Where(x => x.SubscribedEventType == evt);
        }

        var items = await query
            .OrderBy(x => x.CreatedAt)
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
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> SetInstanceSecrets(
        Guid instanceId,
        SetInstanceSecretsRequest request,
        ISecretStore secretStore,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");

        if (request.Secrets is null || request.Secrets.Count == 0)
            return Results.BadRequest("secrets is required");

        var entity = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (entity is null)
            return Results.NotFound();

        Dictionary<string, string>? refs;
        try
        {
            refs = string.IsNullOrWhiteSpace(entity.SecretRefsJson)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.SecretRefsJson);
        }
        catch (JsonException)
        {
            refs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        refs ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in request.Secrets)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Results.BadRequest("secret key must be non-empty");

            // Allow clearing by setting empty/null? For now require a value.
            if (string.IsNullOrWhiteSpace(value))
                return Results.BadRequest($"secret '{key}' value must be non-empty");

            var reference = await secretStore.SetInstanceSecretAsync(instanceId, key, value, ct);
            refs[key] = reference;
        }

        entity.SecretRefsJson = JsonSerializer.Serialize(refs);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(entity));
    }


    private static async Task<IResult> StartRun(
        Guid instanceId,
        StartRunRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");

        var instance = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (instance is null)
            return Results.NotFound();

        if (!instance.Enabled)
            return Results.Conflict("instance is disabled");

        var now = DateTimeOffset.UtcNow;

        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId;

        var run = new Run
        {
            RunId = Guid.NewGuid(),
            InstanceId = instanceId,
            CorrelationId = correlationId,
            Status = "queued",
            CreatedAt = now
        };

        db.Runs.Add(run);
        await db.SaveChangesAsync(ct);

        return Results.Accepted($"/api/instances/{instanceId}/runs", ToResponse(run));
    }


    private static async Task<IResult> ManualInvoke(
        Guid instanceId,
        StartRunRequest request,
        ServiceBusClientWrapper serviceBusClient,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");

        var instance = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (instance is null)
            return Results.NotFound();

        if (!instance.Enabled)
            return Results.Conflict("instance is disabled");


        var queueMessage = new ProcessPayload
        {
            instanceId = instanceId,
        };

        await serviceBusClient.SendAsync(queueMessage, ct);


        return Results.Accepted($"/api/instances/{instanceId}/runs", ToResponse(instance));
    }

    private static async Task<IResult> UpdateRun(
        Guid runId,
        UpdateRunRequest request,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (runId == Guid.Empty)
            return Results.BadRequest("runId is required");

        var entity = await db.Runs
            .SingleOrDefaultAsync(x => x.RunId == runId, ct);

        if (entity is null)
            return Results.NotFound();

        if (!string.IsNullOrWhiteSpace(request.Status))
            entity.Status = request.Status;

        if (request.StartedAt is not null)
            entity.StartedAt = request.StartedAt;

        if (request.EndedAt is not null)
            entity.EndedAt = request.EndedAt;

        if (request.Error is not null)
            entity.Error = string.IsNullOrWhiteSpace(request.Error) ? null : request.Error;

        if (request.OutputFullBlobPath is not null)
            entity.OutputFullBlobPath = string.IsNullOrWhiteSpace(request.OutputFullBlobPath) ? null : request.OutputFullBlobPath;

        if (request.OutputContentType is not null)
            entity.OutputContentType = string.IsNullOrWhiteSpace(request.OutputContentType) ? null : request.OutputContentType;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(entity));
    }

    private static async Task<IResult> ListRuns(
        Guid instanceId,
        int? page,
        int? pageSize,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");

        var instanceExists = await db.IntegrationInstances
            .AnyAsync(x => x.InstanceId == instanceId, ct);

        if (!instanceExists)
            return Results.NotFound();

        var p = page.GetValueOrDefault(1);
        if (p < 1) p = 1;

        var ps = pageSize.GetValueOrDefault(25);
        if (ps < 1) ps = 1;
        if (ps > 200) ps = 200;

        var baseQuery = db.Runs
            .Where(x => x.InstanceId == instanceId);

        var total = await baseQuery.CountAsync(ct);

        var items = await (
            from r in baseQuery
            join i in db.IntegrationInstances.AsNoTracking() on r.InstanceId equals i.InstanceId
            select new { r, i }
        )
            .OrderByDescending(x => x.r.CreatedAt)
            .Skip((p - 1) * ps)
            .Take(ps)
            .Select(x => new RunResponse(
                x.r.RunId,
                x.r.InstanceId,
                x.r.CorrelationId,
                x.r.Status,
                x.r.StartedAt,
                x.r.EndedAt,
                x.r.Error,
                x.r.OutputFullBlobPath,
                x.r.OutputContentType,
                x.r.CreatedAt,
                x.i.TriggerType,
                x.i.SubscribedEventType,
                x.i.IntegrationTarget))
            .ToListAsync(ct);

        return Results.Ok(new PagedResponse<RunResponse>(items, p, ps, total));
    }

    private static async Task<IResult> ListAllRuns(
        RuntimeDbContext db,
        CancellationToken ct)
    {
        const int limit = 100;

        var items = await (
            from r in db.Runs
            join i in db.IntegrationInstances.AsNoTracking() on r.InstanceId equals i.InstanceId
            select new { r, i }
        )
            .OrderByDescending(x => x.r.CreatedAt)
            .Take(limit)
            .Select(x => new RunResponse(
                x.r.RunId,
                x.r.InstanceId,
                x.r.CorrelationId,
                x.r.Status,
                x.r.StartedAt,
                x.r.EndedAt,
                x.r.Error,
                x.r.OutputFullBlobPath,
                x.r.OutputContentType,
                x.r.CreatedAt,
                x.i.TriggerType,
                x.i.SubscribedEventType,
                x.i.IntegrationTarget))
            .ToListAsync(ct);

        return Results.Ok(items);
    }

    private static async Task<IResult> DeleteInstance(
        Guid instanceId,
        RuntimeDbContext db,
        CancellationToken ct)
    {
        if (instanceId == Guid.Empty)
            return Results.BadRequest("instanceId is required");

        var entity = await db.IntegrationInstances
            .SingleOrDefaultAsync(x => x.InstanceId == instanceId, ct);

        if (entity is null)
            return Results.NotFound();

        if (entity.DeletedAt is null)
        {
            entity.DeletedAt = DateTimeOffset.UtcNow;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.NoContent();
    }

    private static InstanceResponse ToResponse(IntegrationInstance entity) => new(
        entity.InstanceId,
        entity.CustomerId,
        entity.CustomerName,
        entity.IntegrationTarget,
        entity.SubscribedEventType,
        entity.Enabled,
        string.IsNullOrWhiteSpace(entity.DisplayName) ? entity.IntegrationTarget : entity.DisplayName,
        entity.IntegrationTargetParametersJson,
        entity.EventParametersJson,
        entity.SecretRefsJson,
        entity.CreatedAt,
        entity.UpdatedAt,
        entity.TriggerType,
        entity.ScheduleCron,
        entity.ScheduleTimeZone,
        entity.NextDueAtUtc,
        entity.ScheduleVersion);

    private static RunResponse ToResponse(Run entity) => new(
        entity.RunId,
        entity.InstanceId,
        entity.CorrelationId,
        entity.Status,
        entity.StartedAt,
        entity.EndedAt,
        entity.Error,
        entity.OutputFullBlobPath,
        entity.OutputContentType,
        entity.CreatedAt);

    private static async Task<bool> IsAllowedScheduleTimeZoneAsync(RuntimeDbContext db, string? tz, CancellationToken ct)
    {
        var v = (tz ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(v))
            return false;

        return await db.ScheduleTimeZones
            .AsNoTracking()
            .AnyAsync(x => x.Enabled && x.Id == v, ct);
    }
}

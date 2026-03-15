using Icp.Persistence.Data.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Icp.Api.Host.Endpoints;

public static class IntegrationAccountsEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            var prod_grp = app.MapGroup("/api")
                .WithTags("IntegrationAccounts");

            Map(prod_grp, requireAuth: true);

            return app;
        }
        else
        {
            var dev_grp = app.MapGroup("/api")
                .WithTags("IntegrationAccounts");

            Map(dev_grp, requireAuth: false);

            return app;
        }
    }

    private static void Map(RouteGroupBuilder grp, bool requireAuth)
    {
        var b = requireAuth ? grp.RequireAuthorization("UI") : grp;

        b.MapGet("/integrationaccounts", ListAccounts)
            .WithName("ListIntegrationAccounts");

        b.MapGet("/integrationaccounts/{id:guid}", GetAccount)
            .WithName("GetIntegrationAccount");

        b.MapPost("/integrationaccounts", CreateAccount)
            .WithName("CreateIntegrationAccount");

        b.MapPut("/integrationaccounts/{id:guid}", UpdateAccount)
            .WithName("UpdateIntegrationAccount");

        b.MapPost("/integrationaccounts/{id:guid}/enable", EnableAccount)
            .WithName("EnableIntegrationAccount");

        b.MapPost("/integrationaccounts/{id:guid}/disable", DisableAccount)
            .WithName("DisableIntegrationAccount");

        b.MapGet("/integrationaccounts/{id:guid}/instances", ListInstances)
            .WithName("ListIntegrationAccountInstances");
    }

    private sealed record IntegrationAccountResponse(
        Guid AccountId,
        string DisplayName,
        string ExternalCustomerId,
        bool Enabled,
        string InboundKeyHash,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record IntegrationInstanceSummaryResponse(
        Guid InstanceId,
        Guid AccountId,
        string CustomerId,
        string CustomerName,
        string IntegrationTarget,
        string DisplayName,
        bool Enabled,
        string SubscribedEventType,
        string TriggerType,
        string? ScheduleCron,
        string? ScheduleTimeZone,
        DateTimeOffset? NextDueAtUtc,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);

    private sealed record CreateIntegrationAccountRequest(
        string DisplayName,
        string ExternalCustomerId,
        bool Enabled,
        string InboundKeyHash);

    private sealed record UpdateIntegrationAccountRequest(
        string DisplayName,
        string ExternalCustomerId,
        bool Enabled,
        string InboundKeyHash);

    private static IntegrationAccountResponse ToResponse(IntegrationAccount a) => new(
        a.AccountId,
        a.DisplayName,
        a.ExternalCustomerId,
        a.Enabled,
        a.InboundKeyHash,
        a.CreatedAt,
        a.UpdatedAt);

    private static IntegrationInstanceSummaryResponse ToSummary(IntegrationInstance x) => new(
        x.InstanceId,
        x.AccountId,
        x.CustomerId,
        x.CustomerName,
        x.IntegrationTarget,
        x.DisplayName,
        x.Enabled,
        x.SubscribedEventType,
        x.TriggerType,
        x.ScheduleCron,
        x.ScheduleTimeZone,
        x.NextDueAtUtc,
        x.CreatedAt,
        x.UpdatedAt);

    private static async Task<IResult> ListAccounts(RuntimeDbContext db, CancellationToken ct)
    {
        var accounts = await db.IntegrationAccounts
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => ToResponse(x))
            .ToListAsync(ct);

        return Results.Ok(accounts);
    }

    private static async Task<IResult> GetAccount(Guid id, RuntimeDbContext db, CancellationToken ct)
    {
        var account = await db.IntegrationAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AccountId == id, ct);

        return account is null ? Results.NotFound() : Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> CreateAccount(CreateIntegrationAccountRequest request, RuntimeDbContext db, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalCustomerId))
            return Results.BadRequest("externalCustomerId is required");

        var externalCustomerId = request.ExternalCustomerId.Trim();

        var exists = await db.IntegrationAccounts
            .AnyAsync(x => x.ExternalCustomerId == externalCustomerId, ct);

        if (exists)
            return Results.Conflict("externalCustomerId must be unique");

        var now = DateTimeOffset.UtcNow;

        var account = new IntegrationAccount
        {
            AccountId = Guid.NewGuid(),
            DisplayName = (request.DisplayName ?? string.Empty).Trim(),
            ExternalCustomerId = externalCustomerId,
            Enabled = request.Enabled,
            InboundKeyHash = (request.InboundKeyHash ?? string.Empty).Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.IntegrationAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/api/integrationaccounts/{account.AccountId}", ToResponse(account));
    }

    private static async Task<IResult> UpdateAccount(Guid id, UpdateIntegrationAccountRequest request, RuntimeDbContext db, CancellationToken ct)
    {
        var account = await db.IntegrationAccounts.SingleOrDefaultAsync(x => x.AccountId == id, ct);
        if (account is null)
            return Results.NotFound();

        if (string.IsNullOrWhiteSpace(request.ExternalCustomerId))
            return Results.BadRequest("externalCustomerId is required");

        var externalCustomerId = request.ExternalCustomerId.Trim();

        var externalIdTaken = await db.IntegrationAccounts
            .AnyAsync(x => x.AccountId != id && x.ExternalCustomerId == externalCustomerId, ct);

        if (externalIdTaken)
            return Results.Conflict("externalCustomerId must be unique");

        account.DisplayName = (request.DisplayName ?? string.Empty).Trim();
        account.ExternalCustomerId = externalCustomerId;
        account.Enabled = request.Enabled;
        account.InboundKeyHash = (request.InboundKeyHash ?? string.Empty).Trim();
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> EnableAccount(Guid id, RuntimeDbContext db, CancellationToken ct)
    {
        var account = await db.IntegrationAccounts.SingleOrDefaultAsync(x => x.AccountId == id, ct);
        if (account is null)
            return Results.NotFound();

        if (!account.Enabled)
        {
            account.Enabled = true;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> DisableAccount(Guid id, RuntimeDbContext db, CancellationToken ct)
    {
        var account = await db.IntegrationAccounts.SingleOrDefaultAsync(x => x.AccountId == id, ct);
        if (account is null)
            return Results.NotFound();

        if (account.Enabled)
        {
            account.Enabled = false;
            account.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(ToResponse(account));
    }

    private static async Task<IResult> ListInstances(Guid id, RuntimeDbContext db, CancellationToken ct)
    {
        var exists = await db.IntegrationAccounts
            .AsNoTracking()
            .AnyAsync(x => x.AccountId == id, ct);

        if (!exists)
            return Results.NotFound();

        var instances = await db.IntegrationInstances
            .AsNoTracking()
            .Where(x => x.AccountId == id)
            .OrderByDescending(x => x.UpdatedAt)
            .Select(x => ToSummary(x))
            .ToListAsync(ct);

        return Results.Ok(instances);
    }
}

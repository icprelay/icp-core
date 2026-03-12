using Icp.Persistence.Data.Runtime;
using Icp.Contracts.EventTypes;
using Microsoft.EntityFrameworkCore;

namespace Icp.Api.Host.Endpoints;

public static class EventTypesEndpoints
{
    public static IEndpointRouteBuilder MapEventTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            #region production
            var prod_grp = app.MapGroup("/api")
        .RequireAuthorization("UI")
        .WithTags("EventTypes");

            prod_grp.MapGet("/eventtypes", ListEventTypes)
                .WithName("ListEventTypes");

            return app;
            #endregion
        }
        else
        {
            #region development
            var dev_grp = app.MapGroup("/api")
                .WithTags("EventTypes");

            dev_grp.MapGet("/eventtypes", ListEventTypes)
                .WithName("ListEventTypes");

            return app;
            #endregion
        }
    }

    private static async Task<IResult> ListEventTypes(string? triggerType, RuntimeDbContext db, CancellationToken ct)
    {
        IQueryable<EventType> query = db.EventTypes;

        if (!string.IsNullOrWhiteSpace(triggerType))
        {
            var tt = triggerType.Trim();
            query = query.Where(x => x.AllowedTriggerTypes.Contains(tt));
        }

        var items = await query
            .OrderBy(x => x.Name)
            .Select(x => new EventTypeResponse(
                x.Name,
                x.ParametersTemplateJson,
                x.DisplayName,
                x.IconKey))
            .ToListAsync(ct);

        return Results.Ok(items);
    }
}

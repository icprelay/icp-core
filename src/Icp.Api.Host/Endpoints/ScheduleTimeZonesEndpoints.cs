using Icp.Persistence.Data.Runtime;
using Icp.Contracts.Instances;
using Microsoft.EntityFrameworkCore;

namespace Icp.Api.Host.Endpoints;

public static class ScheduleTimeZonesEndpoints
{
    public static IEndpointRouteBuilder MapScheduleTimeZonesEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            var prod_grp = app.MapGroup("/api")
                .RequireAuthorization("UI")
                .WithTags("ScheduleTimeZones");

            prod_grp.MapGet("/scheduletimezones", ListScheduleTimeZones)
                .WithName("ListScheduleTimeZones");

            return app;
        }
        else
        {
            var dev_grp = app.MapGroup("/api")
                .WithTags("ScheduleTimeZones");

            dev_grp.MapGet("/scheduletimezones", ListScheduleTimeZones)
                .WithName("ListScheduleTimeZones");

            return app;
        }
    }

    private static async Task<IResult> ListScheduleTimeZones(RuntimeDbContext db, CancellationToken ct)
    {
        var items = await db.ScheduleTimeZones
            .AsNoTracking()
            .Where(x => x.Enabled)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new ScheduleTimeZoneResponse(x.Id, x.DisplayName))
            .ToListAsync(ct);

        return Results.Ok(items);
    }
}

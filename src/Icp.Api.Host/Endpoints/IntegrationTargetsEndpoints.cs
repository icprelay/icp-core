using Icp.Persistence.Data.Runtime;
using Icp.Contracts.IntegrationTargets;
using Microsoft.EntityFrameworkCore;

namespace Icp.Api.Host.Endpoints;

public static class IntegrationTargetsEndpoints
{
    public static IEndpointRouteBuilder MapIntegrationTargetsEndpoints(this IEndpointRouteBuilder app)
    {
        var env = app.GetAspNetCoreEnvironment();

        if (env == "Production")
        {
            #region production
            var prod_grp = app.MapGroup("/api")
                .RequireAuthorization("UI")
                .WithTags("IntegrationTargets");

            prod_grp.MapGet("/integrationtargets", ListIntegrationTargets)
                .WithName("ListIntegrationTargets");

            return app;
            #endregion
        }
        else
        {
            #region development
            var dev_grp = app.MapGroup("/api")
                .WithTags("IntegrationTargets");

            dev_grp.MapGet("/integrationtargets", ListIntegrationTargets)
                .WithName("ListIntegrationTargets");

            return app;
            #endregion
        }
    }

    private static async Task<IResult> ListIntegrationTargets(string? triggerType, RuntimeDbContext db, CancellationToken ct)
    {
        IQueryable<IntegrationTarget> query = db.IntegrationTargets;

        if (!string.IsNullOrWhiteSpace(triggerType))
        {
            var tt = triggerType.Trim();
            query = query.Where(x => x.AllowedTriggerTypes.Contains(tt));
        }

        var items = await query
            .OrderBy(x => x.Name)
            .Select(x => new IntegrationTargetResponse(
                x.Name,
                x.ParametersTemplateJson,
                x.SecretsTemplateJson,
                x.Availability,
                x.DisplayName,
                x.IconKey))
            .ToListAsync(ct);

        return Results.Ok(items);
    }
}

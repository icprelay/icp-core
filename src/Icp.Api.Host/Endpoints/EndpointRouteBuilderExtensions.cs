namespace Icp.Api.Host.Endpoints;

public static class EndpointRouteBuilderExtensions
{
    public static string? GetAspNetCoreEnvironment(this IEndpointRouteBuilder app)
    {
        var configuration = app.ServiceProvider.GetService<IConfiguration>();
        if (configuration is null)
            throw new InvalidOperationException("Configuration service is not available.");

        return configuration["ASPNETCORE_ENVIRONMENT"];
    }
}

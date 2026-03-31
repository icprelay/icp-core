using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Icp.Messaging.ServiceBus;
using Icp.Persistence.Data.Runtime;
using Icp.Storage;
using Icp.Storage.Abstractions;
using Icp.Storage.Azure;
using Icp.Api.Host.Endpoints;
using Icp.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwtBearerOptions =>
{
    var apiClientId = builder.Configuration["AzureAd:ClientId"];
    var apiTenantId = builder.Configuration["AzureAd:TenantId"];
    jwtBearerOptions.TokenValidationParameters.ValidAudiences = new[]
    {
        apiClientId,
        $"api://{apiClientId}"
    };
    jwtBearerOptions.TokenValidationParameters.ValidIssuers = new[]
    {
        $"https://sts.windows.net/{apiTenantId}/",                 // v1 (Managed Identity often uses this)
        $"https://login.microsoftonline.com/{apiTenantId}/v2.0"    // v2
    };

});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UI", p => p.RequireAssertion(ctx =>
    {
        return ctx.User.HasClaim("scp", "icp.access")
            || ctx.User.HasClaim("http://schemas.microsoft.com/identity/claims/scope", "icp.access");
    }));

    options.AddPolicy("Worker", p => p.RequireRole("icp.worker"));

    options.AddPolicy("WorkerOrUI", policy =>
    {
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("icp.worker") ||
            ctx.User.HasClaim("scp", "icp.access") ||
            ctx.User.HasClaim("http://schemas.microsoft.com/identity/claims/scope", "icp.access"));
    });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<RuntimeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RuntimeDb")));

builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<IBlobStorageService>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
    return new AzureBlobStorageService(opts);
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Integration Control Plane API", Version = "v1" });

    options.AddSecurityDefinition(
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter a valid token.",
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        BearerFormat = "JWT",
                        Scheme = "Bearer",
                    }
                );

    options.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
});

builder.Services.Configure<KeyVaultOptions>(builder.Configuration.GetSection(KeyVaultOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyVaultOptions>>().Value;
    if (string.IsNullOrWhiteSpace(opts.VaultUri))
        throw new InvalidOperationException("KeyVault:VaultUri configuration is required.");

    return new SecretClient(new Uri(opts.VaultUri), new DefaultAzureCredential());
});
builder.Services.AddSingleton(_ => new ServiceBusClientWrapper(builder.Configuration));
builder.Services.AddScoped<ISecretStore>(sp =>
{
    var client = sp.GetRequiredService<SecretClient>();
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeyVaultOptions>>().Value;
    return new KeyVaultSecretStore(client, opts);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("ok"));
app.MapGet("/instances", () => Results.Ok()).RequireAuthorization("UI");
app.MapGet("/internal/ingest", () => Results.Ok()).RequireAuthorization("Worker");
app.MapGet("/debug/claims", (ClaimsPrincipal user) =>
    user.Claims.Select(c => new { c.Type, c.Value })).RequireAuthorization();

app.MapInstanceEndpoints();
app.MapIntegrationAccountsEndpoints();
app.MapIntegrationTargetsEndpoints();
app.MapEventTypesEndpoints();
app.MapScheduleTimeZonesEndpoints();
app.MapSchedulerEndpoints();
app.MapEventTracesEndpoints();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("v1/swagger.json", "Integration Control Plane API");
    });
}

app.MapControllers();

app.Run();

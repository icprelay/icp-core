using Icp.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class IndexModel(IcpApiClient apiClient) : PageModel
{
    public string Error { get; private set; } = string.Empty;

    public int ActiveIntegrationsCount { get; private set; }
    public int PausedIntegrationsCount { get; private set; }
    public TimeSpan? TimeSinceLastRun { get; private set; }
    public int RunsLast24Hours { get; private set; }
    public int FailuresLast24Hours { get; private set; }

    public bool ApiHealthy { get; private set; }

    public async Task<IActionResult> OnGet(CancellationToken ct)
    {
        try
        {
            await LoadSystemStatusAsync(ct);
            return Page();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
    }

    private async Task LoadSystemStatusAsync(CancellationToken ct)
    {
        ActiveIntegrationsCount = 0;
        PausedIntegrationsCount = 0;
        TimeSinceLastRun = null;
        RunsLast24Hours = 0;
        FailuresLast24Hours = 0;
        ApiHealthy = false;

        try
        {
            await apiClient.PingHealthAsync(ct);
            ApiHealthy = true;
        }
        catch
        {
            ApiHealthy = false;
            return;
        }

        var all = await apiClient.ListAllHumanInstancesAsync(customerId: null, subscribedEventType: null, ct);

        ActiveIntegrationsCount = all.Count(x => x.Enabled);
        PausedIntegrationsCount = all.Count(x => !x.Enabled);

        DateTimeOffset? latestRunCreatedAt = null;
        var since = DateTimeOffset.UtcNow.AddHours(-24);
        var totalRuns = 0;
        var failures = 0;

        foreach (var instance in all)
        {
            var runs = await apiClient.ListRunsAsync(instance.InstanceId, ct);
            if (runs.Count == 0)
                continue;

            var newest = runs.MaxBy(r => r.CreatedAt);
            if (newest is not null && (latestRunCreatedAt is null || newest.CreatedAt > latestRunCreatedAt.Value))
                latestRunCreatedAt = newest.CreatedAt;

            var last24h = runs.Where(r => r.CreatedAt >= since).ToList();
            totalRuns += last24h.Count;
            failures += last24h.Count(r => string.Equals(r.Status, "failed", StringComparison.OrdinalIgnoreCase));
        }

        if (latestRunCreatedAt is not null)
            TimeSinceLastRun = DateTimeOffset.UtcNow - latestRunCreatedAt.Value;

        RunsLast24Hours = totalRuns;
        FailuresLast24Hours = failures;
    }
}

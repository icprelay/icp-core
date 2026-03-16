using System.Text.Json;
using Icp.Contracts.Instances;
using Icp.Contracts.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages.Instances;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class IndexModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool All { get; set; }

    [BindProperty]
    public bool AllPost { get; set; }

    public IReadOnlyList<InstanceListItemViewModel> Items { get; private set; } = [];

    public string Error { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGet(CancellationToken ct)
    {
        try
        {
            await LoadAsync(ct);
            return Page();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
    }

    public async Task<IActionResult> OnPostEnableAsync(Guid instanceId, CancellationToken ct)
    {
        All = AllPost;
        try
        {
            await apiClient.EnableInstanceAsync(instanceId, ct);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableAsync(Guid instanceId, CancellationToken ct)
    {
        All = AllPost;
        try
        {
            await apiClient.DisableInstanceAsync(instanceId, ct);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }

        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRunNowAsync(Guid instanceId, CancellationToken ct)
    {
        All = AllPost;
        try
        {
            await apiClient.StartRunAsync(instanceId, correlationId: null, ct);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }

        await LoadAsync(ct);
        return Page();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Error = string.Empty;

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            CustomerId = User.GetObjectId();
        }

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            Items = [];
            Error = "Unable to resolve customerId.";
            return;
        }

        try
        {
            var targets = await apiClient.ListIntegrationTargetsAsync(ct);
            var integrationTargetMetadata = targets
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .GroupBy(t => t.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        var displayName = string.IsNullOrWhiteSpace(first.DisplayName) ? g.Key : first.DisplayName!;
                        var iconKey = first.IconKey ?? string.Empty;
                        var secretsTemplate = first.SecretsTemplateJson ?? "{}";
                        return new IntegrationTargetMetadata(displayName, iconKey, secretsTemplate);
                    },
                    StringComparer.OrdinalIgnoreCase);

            var eventTypes = await apiClient.ListEventTypesAsync(triggerType: null, ct);

            var eventTypeMetadata = eventTypes
                .Where(t => !string.IsNullOrWhiteSpace(t.Name))
                .GroupBy(t => t.Name!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var first = g.First();
                        var displayName = string.IsNullOrWhiteSpace(first.DisplayName) ? g.Key : first.DisplayName!;
                        var iconKey = first.IconKey ?? string.Empty;
                        return new EventTypeMetadata(displayName, iconKey);
                    },
                    StringComparer.OrdinalIgnoreCase);

            var instances = await apiClient.ListAllInstancesAsync(subscribedEventType: null, ct);

            var items = new List<InstanceListItemViewModel>(instances.Count);

            foreach (var instance in instances)
            {
                RunResponse? lastRun = null;
                try
                {
                    var runs = await apiClient.ListRunsAsync(instance.InstanceId, ct);
                    lastRun = runs.FirstOrDefault();
                }
                catch
                {
                    lastRun = null;
                }

                var eventTypeName = instance.SubscribedEventType ?? string.Empty;
                eventTypeMetadata.TryGetValue(eventTypeName, out var eventMeta);

                var targetName = instance.IntegrationTarget ?? string.Empty;
                integrationTargetMetadata.TryGetValue(targetName, out var targetMeta);

                var requiresSecrets = targetMeta is not null && RequiresSecrets(targetMeta.SecretsTemplateJson);

                items.Add(new InstanceListItemViewModel(
                    instance,
                    lastRun,
                    eventMeta?.DisplayName ?? eventTypeName,
                    eventMeta?.IconKey ?? string.Empty,
                    targetMeta?.DisplayName ?? targetName,
                    targetMeta?.IconKey ?? string.Empty,
                    requiresSecrets));
            }

            Items = items
                .OrderBy(c => c.Instance.CustomerId)
                .ToList();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            Items = [];
            TempData[AuthRefreshTempData.Key] = true;
            Error = "Authentication is required. Click 'Refresh credentials' to sign in again.";
        }
        catch (Exception ex)
        {
            Items = [];
            Error = ex.Message;
            if (ex.Message.Contains("IDW10502", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("MsalUiRequiredException", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("MsalUiRequired", StringComparison.OrdinalIgnoreCase))
            {
                TempData[AuthRefreshTempData.Key] = true;
            }
        }
    }

    public TimeSpan GetTimeZoneOffset(string? timeZoneId, DateTime utcDateTime)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return TimeSpan.Zero;

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return tz.GetUtcOffset(utcDateTime);
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }

    private static bool RequiresSecrets(string? secretsTemplateJson)
    {
        if (string.IsNullOrWhiteSpace(secretsTemplateJson))
            return false;

        var trimmed = secretsTemplateJson.Trim();
        if (string.Equals(trimmed, "{}", StringComparison.Ordinal))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            return doc.RootElement.ValueKind != JsonValueKind.Object || doc.RootElement.EnumerateObject().Any();
        }
        catch
        {
            return true;
        }
    }

    private sealed record IntegrationTargetMetadata(string DisplayName, string IconKey, string SecretsTemplateJson);

    private sealed record EventTypeMetadata(string DisplayName, string IconKey);
}

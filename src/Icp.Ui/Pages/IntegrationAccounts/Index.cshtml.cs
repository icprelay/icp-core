using Icp.Contracts.IntegrationAccounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages.IntegrationAccounts;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class IndexModel(IcpApiClient apiClient) : PageModel
{
    public IReadOnlyList<IntegrationAccountListItemViewModel> Items { get; private set; } = [];

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

    public async Task<IActionResult> OnPostEnableAsync(Guid accountId, CancellationToken ct)
    {
        try
        {
            await apiClient.EnableIntegrationAccountAsync(accountId, ct);
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

    public async Task<IActionResult> OnPostDisableAsync(Guid accountId, CancellationToken ct)
    {
        try
        {
            await apiClient.DisableIntegrationAccountAsync(accountId, ct);
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

        try
        {
            var accounts = await apiClient.ListIntegrationAccountsAsync(ct);

            var instanceCounts = new Dictionary<Guid, int>();
            foreach (var a in accounts)
            {
                try
                {
                    var instances = await apiClient.ListIntegrationAccountInstancesAsync(a.AccountId, ct);
                    instanceCounts[a.AccountId] = instances.Count;
                }
                catch
                {
                    instanceCounts[a.AccountId] = 0;
                }
            }

            Items = accounts
                .OrderByDescending(x => x.UpdatedAt)
                .Select(a => new IntegrationAccountListItemViewModel(
                    a,
                    instanceCounts.TryGetValue(a.AccountId, out var c) ? c : 0))
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

    public sealed record IntegrationAccountListItemViewModel(IntegrationAccountResponse Account, int InstanceCount);
}

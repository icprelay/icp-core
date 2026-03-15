using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages.IntegrationAccounts;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class CreateModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty]
    public string DisplayName { get; set; } = string.Empty;

    [BindProperty]
    public string ExternalCustomerId { get; set; } = string.Empty;

    [BindProperty]
    public bool Enabled { get; set; } = true;

    [BindProperty]
    public string InboundKeyHash { get; set; } = string.Empty;

    public string Error { get; private set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Error = string.Empty;

        if (string.IsNullOrWhiteSpace(ExternalCustomerId))
        {
            Error = "External customer id is required.";
            return Page();
        }

        try
        {
            var created = await apiClient.CreateIntegrationAccountAsync(
                externalCustomerId: ExternalCustomerId.Trim(),
                displayName: (DisplayName ?? string.Empty).Trim(),
                enabled: Enabled,
                inboundKeyHash: (InboundKeyHash ?? string.Empty).Trim(),
                ct);

            return RedirectToPage("/IntegrationAccounts/Index");
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (HttpRequestException ex)
        {
            Error = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            if (ex.Message.Contains("IDW10502", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("MsalUiRequiredException", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("MsalUiRequired", StringComparison.OrdinalIgnoreCase))
            {
                TempData[AuthRefreshTempData.Key] = true;
            }

            return Page();
        }
    }
}

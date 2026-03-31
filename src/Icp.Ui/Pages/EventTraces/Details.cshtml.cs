using Icp.Contracts.EventTraces;
using Icp.Ui.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages.EventTraces;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class DetailsModel(IcpApiClient apiClient) : PageModel
{
    public EventTraceResponse? Trace { get; private set; }
    public IReadOnlyList<EventStepResponse> Steps { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid eventId, CancellationToken ct)
    {
        if (eventId == Guid.Empty)
            return RedirectToPage("/Index");

        try
        {
            Trace = await apiClient.GetEventTraceAsync(eventId, ct);

            if (Trace is null)
            {
                ErrorMessage = $"Event trace with ID '{eventId}' not found.";
                return Page();
            }

            Steps = await apiClient.ListEventStepsAsync(eventId, ct);

            return Page();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load event trace: {ex.Message}";
            return Page();
        }
    }
}

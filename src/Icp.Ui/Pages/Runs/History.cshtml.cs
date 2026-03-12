using Icp.Contracts.Runs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace Icp.Ui.Pages.Runs;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class HistoryModel(IcpApiClient apiClient) : PageModel
{
    public IReadOnlyList<RunResponse> Runs { get; private set; } = [];

    public string Error { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try
        {
            Runs = await apiClient.ListAllRunsLatestAsync(ct);
            return Page();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDownloadRunOutputAsync(string runId, string blobPath, string contentType, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(runId) || runId == Guid.Empty.ToString())
        {
            Error = "Run ID is required.";
            return Page();
        }

        try
        {
            var result = await apiClient.DownloadRunOutputAsync(runId, ct);
            return File(result, contentType, blobPath);
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }

}

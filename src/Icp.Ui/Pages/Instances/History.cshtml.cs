using Icp.Contracts.Instances;
using Icp.Contracts.Runs;
using Icp.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using Icp.Ui;

namespace Integration.ControlPlane.Pages.Instances;

[Authorize]
[AuthorizeForScopes(ScopeKeySection = "IcpApi:Scope")]
public sealed class HistoryModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid InstanceId { get; set; }

    public InstanceResponse? Instance { get; private set; }

    public PagedResponse<RunResponse> Runs { get; private set; } = new([], 1, 25, null);

    public string Error { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InstanceId == Guid.Empty)
        {
            Error = "instanceId is required.";
            return Page();
        }

        try
        {
            Instance = await apiClient.GetInstanceAsync(InstanceId, ct);
            if (Instance is null)
                return NotFound();

            CustomerId ??= Instance.CustomerId;

            Runs = await apiClient.ListRunsPagedAsync(InstanceId, page: 1, pageSize: 25, ct);
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

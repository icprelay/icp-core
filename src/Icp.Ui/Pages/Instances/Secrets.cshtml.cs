using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Text.Json;

namespace Icp.Ui.Pages.Instances;

[Authorize]
public sealed class SecretsModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid InstanceId { get; set; }

    [BindProperty]
    public string SecretsText { get; set; } = string.Empty;

    public string Error { get; private set; } = string.Empty;

    public string Message { get; private set; } = string.Empty;

    public string SecretsTemplateText { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (InstanceId == Guid.Empty)
        {
            Error = "InstanceId is required.";
            return Page();
        }

        try
        {
            var instance = await apiClient.GetInstanceAsync(InstanceId, ct);
            if (instance is null)
                return NotFound();

            CustomerId ??= instance.CustomerId;

            var targets = await apiClient.ListIntegrationTargetsAsync(ct);
            var target = targets.SingleOrDefault(t => string.Equals(t.Name, instance.IntegrationTarget, StringComparison.OrdinalIgnoreCase));

            SecretsText = ToKeyValueTemplate(target?.SecretsTemplateJson);
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (InstanceId == Guid.Empty)
        {
            Error = "InstanceId is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            var instance = await apiClient.GetInstanceAsync(InstanceId, ct);
            CustomerId = instance?.CustomerId;
        }

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            Error = "CustomerId is required.";
            return Page();
        }

        Dictionary<string, string> secrets;
        try
        {
            secrets = ParseSecretsText(SecretsText);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }

        try
        {
            await apiClient.SetInstanceSecretsAsync(InstanceId, secrets, ct);
            Message = "Secrets saved.";
            SecretsText = string.Empty;
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

    private static string ToKeyValueTemplate(string? secretsTemplateJson)
    {
        if (string.IsNullOrWhiteSpace(secretsTemplateJson))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(secretsTemplateJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return string.Empty;

            var lines = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name))
                    continue;
                lines.Add($"{prop.Name}=");
            }

            return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static Dictionary<string, string> ParseSecretsText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new InvalidOperationException("Provide at least one secret in key=value format.");

        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith('#'))
                continue;

            var idx = line.IndexOf('=');
            if (idx <= 0 || idx == line.Length - 1)
                throw new InvalidOperationException("Each line must be in key=value format.");

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Secret key must be non-empty.");
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Secret '{key}' value must be non-empty.");

            dict[key] = value;
        }

        if (dict.Count == 0)
            throw new InvalidOperationException("Provide at least one secret in key=value format.");

        return dict;
    }
}

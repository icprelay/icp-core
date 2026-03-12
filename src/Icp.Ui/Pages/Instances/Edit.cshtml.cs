using System.Linq;
using System.Globalization;
using Icp.Contracts.EventTypes;
using Icp.Contracts.Instances;
using Icp.Contracts.IntegrationTargets;
using Icp.Ui;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Icp.Ui.Pages.Instances;

[Authorize]
public sealed class EditModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid InstanceId { get; set; }

    [BindProperty]
    public string IntegrationTarget { get; set; } = string.Empty;

    [BindProperty]
    public string SubscribedEventType { get; set; } = string.Empty;

    [BindProperty]
    public string ParametersJson { get; set; } = "{}";

    [BindProperty]
    public string ParametersText { get; set; } = string.Empty;

    [BindProperty]
    public string EventTypeParametersJson { get; set; } = "{}";

    [BindProperty]
    public string EventTypeParametersText { get; set; } = string.Empty;

    [BindProperty]
    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyList<(string Value, string Label)> ScheduleCronPresets { get; } =
    [
        ("06:00", "06:00"),
        ("07:00", "07:00"),
        ("08:00", "08:00"),
        ("09:00", "09:00"),
        ("10:00", "10:00"),
        ("11:00", "11:00"),
        ("12:00", "12:00"),
        ("13:00", "13:00"),
        ("14:00", "14:00"),
        ("15:00", "15:00"),
        ("16:00", "16:00"),
        ("17:00", "17:00"),
        ("18:00", "18:00")
    ];

    [BindProperty]
    public string? ScheduleCronPreset { get; set; }

    private const string OnDemandDateFormat = "dd-MM-yyyy";

    [BindProperty]
    public string TriggerType { get; set; } = "Event";

    [BindProperty]
    public string? ScheduleTimeZone { get; set; }

    [BindProperty]
    public string[] ScheduleWeekDays { get; set; } = [];

    public IReadOnlyList<ScheduleTimeZoneResponse> ScheduleTimeZones { get; private set; } = [];

    public string Error { get; private set; } = string.Empty;

    public IReadOnlyList<IntegrationTargetResponse> IntegrationTargets { get; private set; } = [];

    public IReadOnlyList<EventTypeResponse> EventTypes { get; private set; } = [];

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

            IntegrationTarget = instance.IntegrationTarget;
            SubscribedEventType = instance.SubscribedEventType;
            DisplayName = instance.DisplayName;

            ParametersJson = instance.IntegrationTargetParametersJson;
            ParametersText = ToKeyValueTemplate(instance.IntegrationTargetParametersJson);

            EventTypeParametersJson = instance.EventParametersJson;
            EventTypeParametersText = ToKeyValueTemplate(instance.EventParametersJson);

            TriggerType = string.IsNullOrWhiteSpace(instance.TriggerType) ? "Event" : instance.TriggerType;
            ScheduleTimeZone = instance.ScheduleTimeZone;
            ScheduleCronPreset = MapCronToPreset(StripWeekDaysFromCron(instance.ScheduleCron));
            ScheduleWeekDays = ParseWeekDaysFromCron(instance.ScheduleCron);

            IntegrationTargets = (await apiClient.ListIntegrationTargetsAsync(TriggerType, ct))
                .Where(t => !string.Equals(t.Availability, "System", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(t.Name, IntegrationTarget, StringComparison.OrdinalIgnoreCase))
                .ToList();

            EventTypes = await apiClient.ListEventTypesAsync(TriggerType, ct);
            ScheduleTimeZones = await apiClient.ListScheduleTimeZonesAsync(ct);

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

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        // Ensure dropdown is populated on validation errors.
        IntegrationTargets = (await apiClient.ListIntegrationTargetsAsync(TriggerType, ct))
            .Where(t => !string.Equals(t.Availability, "System", StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.Name, IntegrationTarget, StringComparison.OrdinalIgnoreCase))
            .ToList();
        EventTypes = await apiClient.ListEventTypesAsync(TriggerType, ct);
        ScheduleTimeZones = await apiClient.ListScheduleTimeZonesAsync(ct);

        var triggerType = string.IsNullOrWhiteSpace(TriggerType) ? "Event" : TriggerType.Trim();

        // If the selected items have no templates, clear their text fields so validation doesn't trip.
        var selectedEventType = EventTypes.FirstOrDefault(et => et.Name.Equals(SubscribedEventType, StringComparison.OrdinalIgnoreCase));
        if (selectedEventType is null || !RequiresParameters(selectedEventType.ParametersTemplateJson))
            EventTypeParametersText = string.Empty;

        var selectedTarget = IntegrationTargets.FirstOrDefault(it => it.Name.Equals(IntegrationTarget, StringComparison.OrdinalIgnoreCase));
        if (selectedTarget is null || !RequiresParameters(selectedTarget.ParametersTemplateJson))
            ParametersText = string.Empty;

        if (string.IsNullOrWhiteSpace(IntegrationTarget))
        {
            Error = "IntegrationTarget is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(SubscribedEventType))
        {
            Error = "SubscribedEventType is required.";
            return Page();
        }

        var eventTypeTemplate = EventTypes.FirstOrDefault(et => et.Name.Equals(SubscribedEventType, StringComparison.OrdinalIgnoreCase));
        if (eventTypeTemplate is null)
        {
            Error = "Invalid Event Type";
            return Page();
        }

        if (triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var requireDates = TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "startDate")
                    || TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "endDate");

                if (requireDates)
                    EnsureOnDemandStartEndDates(EventTypeParametersText, OnDemandDateFormat);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }

        var integrationTargetTemplate = IntegrationTargets.FirstOrDefault(it => it.Name.Equals(IntegrationTarget, StringComparison.OrdinalIgnoreCase));
        if (integrationTargetTemplate is null)
        {
            Error = "Invalid Integration Target";
            return Page();
        }

        var requiresTargetParameters = RequiresParameters(integrationTargetTemplate.ParametersTemplateJson);
        var requiresEventTypeParameters = RequiresParameters(eventTypeTemplate.ParametersTemplateJson);

        if (requiresEventTypeParameters && string.IsNullOrWhiteSpace(EventTypeParametersText))
        {
            Error = "Event type parameters are required.";
            return Page();
        }

        if (requiresTargetParameters && string.IsNullOrWhiteSpace(ParametersText))
        {
            Error = "Integration target parameters are required.";
            return Page();
        }

        try
        {
            // Validate event type parameters against template (types + required keys).
            if (requiresEventTypeParameters)
            {
                ValidateParametersAgainstTemplate(EventTypeParametersText, eventTypeTemplate.ParametersTemplateJson);
            }

            if (triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
            {
                var requireDates = TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "startDate")
                    || TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "endDate");

                if (requireDates)
                    EnsureOnDemandStartEndDates(EventTypeParametersText, OnDemandDateFormat);
            }

            var current = await apiClient.GetInstanceAsync(InstanceId, ct);
            if (current is null)
                return NotFound();

            CustomerId = string.IsNullOrWhiteSpace(CustomerId) ? current.CustomerId : CustomerId;

            var eventTypeParametersJson = requiresEventTypeParameters
                ? ParseKeyValueTextToJson(EventTypeParametersText)
                : "{}";

            var targetParametersJson = requiresTargetParameters
                ? ParseKeyValueTextToJson(ParametersText)
                : "{}";

            var scheduleCron = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                ? ScheduleCronHelpers.ApplyWeekDaysToCron(MapPresetToCron(ScheduleCronPreset), ScheduleWeekDays)
                : null;

            var req = new UpdateInstanceRequest(
                IntegrationTarget,
                SubscribedEventType,
                IntegrationTargetParametersJson: targetParametersJson,
                EventParametersJson: eventTypeParametersJson,
                current.SecretRefsJson,
                DisplayName,
                TriggerType: triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                    ? "Schedule"
                    : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                        ? "OnDemand"
                        : "Event",
                ScheduleCron: scheduleCron,
                ScheduleTimeZone: string.IsNullOrWhiteSpace(ScheduleTimeZone) ? null : ScheduleTimeZone.Trim());

            await apiClient.UpdateInstanceAsync(InstanceId, req, ct);

            return Redirect($"/Instances?customerId={Uri.EscapeDataString(CustomerId)}");
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

    public async Task<IActionResult> OnPostDeleteAsync(CancellationToken ct)
    {
        if (InstanceId == Guid.Empty)
        {
            Error = "InstanceId is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            var current = await apiClient.GetInstanceAsync(InstanceId, ct);
            if (current is not null)
                CustomerId = current.CustomerId;
        }

        try
        {
            await apiClient.DeleteInstanceAsync(InstanceId, ct);
            return Redirect($"/Instances?customerId={Uri.EscapeDataString(CustomerId ?? string.Empty)}");
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

    private static bool RequiresParameters(string? parametersTemplateJson)
    {
        if (string.IsNullOrWhiteSpace(parametersTemplateJson))
            return false;

        var t = parametersTemplateJson.Trim();
        if (t.Equals("{}", StringComparison.Ordinal))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(t);
            return doc.RootElement.ValueKind != JsonValueKind.Object || doc.RootElement.EnumerateObject().Any();
        }
        catch
        {
            return true;
        }
    }

    private static string ToKeyValueTemplate(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return string.Empty;

            var lines = new List<string>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(prop.Name))
                    continue;

                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => prop.Value.GetRawText()
                };

                lines.Add($"{prop.Name}={value}");
            }

            return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ParseKeyValueTextToJson(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new InvalidOperationException("Provide at least one parameter in key=value format.");

        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;
            if (line.StartsWith('#'))
                continue;

            var idx = line.IndexOf('=');
            if (idx <= 0)
                throw new InvalidOperationException("Each line must be in key=value format.");

            var key = line[..idx].Trim();
            var valueText = line[(idx + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Parameter key must be non-empty.");

            // Match secrets behavior: value is required.
            // Keep special-case for messageTemplate to allow an intentionally empty template.
            if (!key.Equals("messageTemplate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(valueText))
                throw new InvalidOperationException($"Parameter '{key}' value must be non-empty.");

            if (key.Equals("messageTemplate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(valueText))
            {
                dict[key] = string.Empty;
                continue;
            }

            dict[key] = valueText;
        }

        if (dict.Count == 0)
            throw new InvalidOperationException("Provide at least one parameter in key=value format.");

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });
    }

    private static object? CoerceScalar(string valueText)
    {
        return valueText;
    }

    private static string? StripWeekDaysFromCron(string? cron)
    {
        var c = (cron ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(c))
            return null;

        var parts = c.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 6)
            return c;

        // Normalize to the base preset cron which always uses "*" for DOW.
        parts[5] = "*";
        return string.Join(' ', parts);
    }

    private string? MapCronToPreset(string? cron)
    {
        var c = (cron ?? string.Empty).Trim();
        return c switch
        {
            "0 0 6 * * *" => "06:00",
            "0 0 7 * * *" => "07:00",
            "0 0 8 * * *" => "08:00",
            "0 0 9 * * *" => "09:00",
            "0 0 10 * * *" => "10:00",
            "0 0 11 * * *" => "11:00",
            "0 0 12 * * *" => "12:00",
            "0 0 13 * * *" => "13:00",
            "0 0 14 * * *" => "14:00",
            "0 0 15 * * *" => "15:00",
            "0 0 16 * * *" => "16:00",
            "0 0 17 * * *" => "17:00",
            "0 0 18 * * *" => "18:00",
            _ => null
        };
    }

    private static string[] ParseWeekDaysFromCron(string? cron)
    {
        var c = (cron ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(c))
            return [];

        var parts = c.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6)
            return [];

        var dow = parts[5].Trim();
        if (string.IsNullOrWhiteSpace(dow) || dow == "*")
            return [];

        var tokens = dow.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in tokens)
        {
            var v = t.Trim();
            if (v.Length == 0)
                continue;

            // Allow both 0-6 and 1-7 (treat 0 as Sunday)
            if (v == "0") v = "7";

            set.Add(v switch
            {
                "1" => "Mon",
                "2" => "Tue",
                "3" => "Wed",
                "4" => "Thu",
                "5" => "Fri",
                "6" => "Sat",
                "7" => "Sun",
                "MON" => "Mon",
                "TUE" => "Tue",
                "WED" => "Wed",
                "THU" => "Thu",
                "FRI" => "Fri",
                "SAT" => "Sat",
                "SUN" => "Sun",
                _ => string.Empty
            });
        }

        return set.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
    }

    private string? MapPresetToCron(string? preset)
    {
        var p = (preset ?? string.Empty).Trim();
        return p switch
        {
            "06:00" => "0 0 6 * * *",
            "07:00" => "0 0 7 * * *",
            "08:00" => "0 0 8 * * *",
            "09:00" => "0 0 9 * * *",
            "10:00" => "0 0 10 * * *",
            "11:00" => "0 0 11 * * *",
            "12:00" => "0 0 12 * * *",
            "13:00" => "0 0 13 * * *",
            "14:00" => "0 0 14 * * *",
            "15:00" => "0 0 15 * * *",
            "16:00" => "0 0 16 * * *",
            "17:00" => "0 0 17 * * *",
            "18:00" => "0 0 18 * * *",
            _ => null
        };
    }

    private static void EnsureOnDemandStartEndDates(string eventTypeParametersText, string format)
    {
        var args = ParseKeyValueTextToDictionary(eventTypeParametersText);

        if (!args.TryGetValue("startDate", out var start) || string.IsNullOrWhiteSpace(start))
            throw new InvalidOperationException($"startDate is required for OnDemand. Expected format: {format}.");

        if (!args.TryGetValue("endDate", out var end) || string.IsNullOrWhiteSpace(end))
            throw new InvalidOperationException($"endDate is required for OnDemand. Expected format: {format}.");

        if (!DateOnly.TryParseExact(start, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startD))
            throw new InvalidOperationException($"startDate must be in format {format}. ");

        if (!DateOnly.TryParseExact(end, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endD))
            throw new InvalidOperationException($"endDate must be in format {format}. ");

        if (startD >= endD)
            throw new InvalidOperationException("startDate must be before endDate.");
    }

    private static Dictionary<string, string> ParseKeyValueTextToDictionary(string input)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = (input ?? string.Empty).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith('#')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0)
                throw new InvalidOperationException("Each line must be in key=value format.");

            var key = line[..idx].Trim();
            var valueText = line[(idx + 1)..].Trim();

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Parameter key must be non-empty.");

            if (!key.Equals("messageTemplate", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(valueText))
                throw new InvalidOperationException($"Parameter '{key}' value must be non-empty.");

            dict[key] = valueText;
        }

        return dict;
    }

    private static void ValidateParametersAgainstTemplate(string parametersText, string? templateJson)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            return;

        var template = templateJson.Trim();
        if (template == "{}")
            return;

        using var doc = JsonDocument.Parse(template);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return;

        var args = ParseKeyValueTextToDictionary(parametersText);

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (!args.TryGetValue(prop.Name, out var rawValue))
                throw new InvalidOperationException($"Event type parameter '{prop.Name}' is required.");

            ValidateScalar(prop.Name, prop.Value, rawValue);
        }

        var allowed = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var k in args.Keys)
        {
            if (!allowed.Contains(k))
                throw new InvalidOperationException($"Unknown event type parameter '{k}'.");
        }
    }

    private static void ValidateScalar(string key, JsonElement templateValue, string raw)
    {
        if (key.Equals("startDate", StringComparison.OrdinalIgnoreCase)
            || key.Equals("endDate", StringComparison.OrdinalIgnoreCase))
        {
            if (!DateOnly.TryParseExact(raw, OnDemandDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                throw new InvalidOperationException($"'{key}' must be a date in format {OnDemandDateFormat}.");
            return;
        }

        switch (templateValue.ValueKind)
        {
            case JsonValueKind.True:
            case JsonValueKind.False:
                if (!bool.TryParse(raw, out _))
                    throw new InvalidOperationException($"'{key}' must be a boolean (true/false).");
                return;

            case JsonValueKind.Number:
                if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _))
                    throw new InvalidOperationException($"'{key}' must be a number.");
                return;

            case JsonValueKind.String:
                return;

            case JsonValueKind.Null:
                return;

            default:
                try
                {
                    JsonDocument.Parse(raw);
                }
                catch
                {
                    throw new InvalidOperationException($"'{key}' must be valid JSON.");
                }
                return;
        }
    }

    private static bool TemplateContainsKey(string? templateJson, string key)
    {
        if (string.IsNullOrWhiteSpace(templateJson))
            return false;

        var t = templateJson.Trim();
        if (t == "{}")
            return false;

        try
        {
            using var doc = JsonDocument.Parse(t);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            return doc.RootElement.TryGetProperty(key, out _);
        }
        catch
        {
            return false;
        }
    }

}

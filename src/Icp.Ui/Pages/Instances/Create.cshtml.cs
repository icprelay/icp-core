using Icp.Contracts.EventTypes;
using Icp.Contracts.Instances;
using Icp.Contracts.IntegrationTargets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Icp.Ui.Pages.Instances;

[Authorize]
public sealed class CreateModel(IcpApiClient apiClient) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? CustomerId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string TriggerType { get; set; } = "Event";

    [BindProperty]
    public string IntegrationTarget { get; set; } = string.Empty;

    [BindProperty]
    public bool Enabled { get; set; } = true;

    [BindProperty]
    public string ParametersJson { get; set; } = "{}";

    [BindProperty]
    public string ParametersText { get; set; } = string.Empty;

    [BindProperty]
    public string EventTypeParametersJson { get; set; } = "{}";

    [BindProperty]
    public string EventTypeParametersText { get; set; } = string.Empty;

    [BindProperty]
    public string SubscribedEventType { get; set; } = string.Empty;

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

    [BindProperty]
    public string? ScheduleTimeZone { get; set; }

    [BindProperty]
    public string[] ScheduleWeekDays { get; set; } = [];

    public IReadOnlyList<ScheduleTimeZoneResponse> ScheduleTimeZones { get; private set; } = [];

    public string Error { get; private set; } = string.Empty;

    public IReadOnlyList<IntegrationTargetResponse> IntegrationTargets { get; private set; } = [];

    public IReadOnlyList<EventTypeResponse> EventTypes { get; private set; } = [];

    private const string OnDemandDateFormat = "dd-MM-yyyy";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        try
        {
            var triggerType = string.IsNullOrWhiteSpace(TriggerType) ? "Event" : TriggerType.Trim();
            TriggerType = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                ? "Schedule"
                : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                    ? "OnDemand"
                    : "Event";

            CustomerId ??= User.GetObjectId();
            IntegrationTargets = (await apiClient.ListIntegrationTargetsAsync(TriggerType, ct))
                .Where(t => !string.Equals(t.Availability, "System", StringComparison.OrdinalIgnoreCase))
                .ToList();
            EventTypes = await apiClient.ListEventTypesAsync(TriggerType, ct);
            ScheduleTimeZones = await apiClient.ListScheduleTimeZonesAsync(ct);
            ParametersText = ToKeyValueTemplate(ParametersJson);
            // Default event type params textarea from selected event type template (if any)
            if (string.IsNullOrWhiteSpace(EventTypeParametersJson) || EventTypeParametersJson.Trim() == "{}")
            {
                var selectedEventType = EventTypes.FirstOrDefault(et => et.Name.Equals(SubscribedEventType, StringComparison.OrdinalIgnoreCase));
                EventTypeParametersJson = selectedEventType?.ParametersTemplateJson ?? "{}";
            }
            EventTypeParametersText = ToKeyValueTemplate(EventTypeParametersJson);

            return Page();
        }
        catch (MicrosoftIdentityWebChallengeUserException)
        {
            TempData[AuthRefreshTempData.Key] = true;
            return Challenge();
        }
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var triggerType = string.IsNullOrWhiteSpace(TriggerType) ? "Event" : TriggerType.Trim();
        TriggerType = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            ? "Schedule"
            : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                ? "OnDemand"
                : "Event";

        // Ensure dropdowns are populated on validation errors.
        IntegrationTargets = (await apiClient.ListIntegrationTargetsAsync(TriggerType, ct))
            .Where(t => !string.Equals(t.Availability, "System", StringComparison.OrdinalIgnoreCase))
            .ToList();
        EventTypes = await apiClient.ListEventTypesAsync(TriggerType, ct);
        ScheduleTimeZones = await apiClient.ListScheduleTimeZonesAsync(ct);

        CustomerId ??= User.GetObjectId();

        if (string.IsNullOrWhiteSpace(CustomerId))
        {
            Error = "Unable to resolve customerId.";
            return Page();
        }

        if (!triggerType.Equals("Event", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            && !triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
        {
            Error = "TriggerType must be 'Event', 'Schedule' or 'OnDemand'.";
            return Page();
        }

        if (triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ScheduleCronPreset))
            {
                Error = "Schedule time is required when TriggerType is 'Schedule'.";
                return Page();
            }
            if (string.IsNullOrWhiteSpace(ScheduleTimeZone))
            {
                Error = "ScheduleTimeZone is required when TriggerType is 'Schedule'.";
                return Page();
            }
            if (ScheduleWeekDays is null || ScheduleWeekDays.Length == 0)
            {
                Error = "Select at least one weekday for the schedule.";
                return Page();
            }
        }

        // Remove early OnDemand start/end enforcement here; we can only decide this after loading the event type template.

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

        var integrationTargetTemplate = IntegrationTargets.FirstOrDefault(it => it.Name.Equals(IntegrationTarget, StringComparison.OrdinalIgnoreCase));

        if(integrationTargetTemplate == null)
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

            // OnDemand: Only enforce startDate/endDate if present in the template.
            if (triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase))
            {
                var requireDates = TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "startDate")
                    || TemplateContainsKey(eventTypeTemplate.ParametersTemplateJson, "endDate");

                if (requireDates)
                    EnsureOnDemandStartEndDates(EventTypeParametersText, OnDemandDateFormat);
            }

            var customerName = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(customerName))
                customerName = CustomerId;

            var eventTypeParametersJson = requiresEventTypeParameters
                ? ParseKeyValueTextToJson(EventTypeParametersText)
                : "{}";

            var targetParametersJson = requiresTargetParameters
                ? ParseKeyValueTextToJson(ParametersText)
                : "{}";

            var scheduleCron = triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                ? ScheduleCronHelpers.ApplyWeekDaysToCron(MapPresetToCron(ScheduleCronPreset), ScheduleWeekDays)
                : null;

            var req = new CreateInstanceRequest(
                IntegrationTarget,
                SubscribedEventType,
                Enabled,
                IntegrationTargetParametersJson: targetParametersJson,
                EventParametersJson: eventTypeParametersJson,
                SecretRefsJson: "{}",
                CustomerName: customerName,
                TriggerType: triggerType.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
                    ? "Schedule"
                    : triggerType.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                        ? "OnDemand"
                        : "Event",
                ScheduleCron: scheduleCron,
                ScheduleTimeZone: string.IsNullOrWhiteSpace(ScheduleTimeZone) ? null : ScheduleTimeZone.Trim());

            await apiClient.CreateInstanceAsync(CustomerId, req, ct);
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

    private static void EnsureOnDemandStartEndDates(string eventTypeParametersText, string format)
    {
        // Require startDate/endDate for OnDemand triggers and validate format.
        // Expected format: dd-MM-yyyy.
        var args = ParseKeyValueTextToDictionary(eventTypeParametersText);

        if (!args.TryGetValue("startDate", out var start) || string.IsNullOrWhiteSpace(start))
            throw new InvalidOperationException($"startDate is required for OnDemand. Expected format: {format}.");

        if (!args.TryGetValue("endDate", out var end) || string.IsNullOrWhiteSpace(end))
            throw new InvalidOperationException($"endDate is required for OnDemand. Expected format: {format}.");

        if (!DateOnly.TryParseExact(start, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startD))
            throw new InvalidOperationException($"startDate must be in format {format}.");

        if (!DateOnly.TryParseExact(end, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endD))
            throw new InvalidOperationException($"endDate must be in format {format}.");

        if (startD >= endD)
            throw new InvalidOperationException("startDate must be before endDate.");
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
            // Treat all keys in template as required.
            if (!args.TryGetValue(prop.Name, out var rawValue))
                throw new InvalidOperationException($"Event type parameter '{prop.Name}' is required.");

            // Enforce value type based on template scalar.
            ValidateScalar(prop.Name, prop.Value, rawValue);
        }

        // Also reject unknown keys (keeps config solid)
        var allowed = doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var k in args.Keys)
        {
            if (!allowed.Contains(k))
                throw new InvalidOperationException($"Unknown event type parameter '{k}'.");
        }
    }

    private static void ValidateScalar(string key, JsonElement templateValue, string raw)
    {
        // Only enforce date format for OnDemand known keys.
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
                // any non-empty string is fine
                return;

            case JsonValueKind.Null:
                return;

            default:
                // For arrays/objects in template, require JSON in the value.
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

    private static string MergeJson(string leftJson, string rightJson)
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        void merge(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                if (parsed is null)
                    return;

                foreach (var kvp in parsed)
                    dict[kvp.Key] = kvp.Value;
            }
            catch
            {
                // Ignore invalid JSON; callers should have produced valid JSON.
            }
        }

        merge(leftJson);
        merge(rightJson);

        return JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });
    }

    private static bool RequiresParameters(string? parametersTemplateJson)
    {
        if (string.IsNullOrWhiteSpace(parametersTemplateJson))
            return false;

        var t = parametersTemplateJson.Trim();
        if (t.Equals("{}", StringComparison.Ordinal))
            return false;

        // Some templates may serialize to an empty object but with whitespace/newlines.
        try
        {
            using var doc = JsonDocument.Parse(t);
            return doc.RootElement.ValueKind != JsonValueKind.Object || doc.RootElement.EnumerateObject().Any();
        }
        catch
        {
            // If template isn't valid JSON, assume parameters exist to stay safe.
            return true;
        }
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
}

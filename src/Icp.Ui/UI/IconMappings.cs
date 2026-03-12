namespace Icp.Ui;

public enum TriggerType
{
    Event,
    Schedule,
    OnDemand
}

public static class IconMappings
{
    public static readonly IReadOnlyDictionary<TriggerType, string> TriggerIconKey = new Dictionary<TriggerType, string>
    {
        { TriggerType.Event, "flash" },
        { TriggerType.Schedule, "clock" },
        { TriggerType.OnDemand, "play" }
    };

    // Maps a logical icon key (used by API/entities) to a Fluent SVG in `wwwroot/svg`.
    // Keep values as relative URLs so they work behind reverse proxies.
    public static readonly IReadOnlyDictionary<string, string> FluentSvgByIconKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Trigger-like
        { "play", "~/svg/ic_fluent_play_32_filled.svg" },
        { "flash", "~/svg/ic_fluent_flash_32_filled.svg" },
        { "clock", "~/svg/ic_fluent_calendar_clock_32_filled.svg" },

        // Common domain icons
        { "database", "~/svg/ic_fluent_database_32_regular.svg" },
        { "add-circle", "~/svg/ic_fluent_add_circle_32_regular.svg" },
        { "check-circle", "~/svg/ic_fluent_checkmark_circle_32_regular.svg" },
        { "sync-circle", "~/svg/ic_fluent_arrow_sync_circle_32_regular.svg" },
        { "document", "~/svg/ic_fluent_document_32_regular.svg" },
        { "document-page", "~/svg/ic_fluent_document_one_page_32_regular.svg" },
        { "shield-check", "~/svg/ic_fluent_shield_checkmark_32_regular.svg" },
        { "calendar-clock", "~/svg/ic_fluent_calendar_clock_32_regular.svg" },
        { "chat", "~/svg/ic_fluent_chat_32_regular.svg" },
        { "folder", "~/svg/ic_fluent_folder_open_28_regular.svg" },
        { "heart-pulse", "~/svg/ic_fluent_heart_pulse_24_regular.svg" },
        { "archive", "~/svg/ic_fluent_archive_24_regular.svg" }
    };

    public static TriggerType ParseTriggerType(string? value)
    {
        var t = (value ?? string.Empty).Trim();

        return t.Equals("Schedule", StringComparison.OrdinalIgnoreCase)
            ? TriggerType.Schedule
            : t.Equals("OnDemand", StringComparison.OrdinalIgnoreCase)
                ? TriggerType.OnDemand
                : TriggerType.Event;
    }

    public static string GetTriggerIconKey(string? triggerType)
    {
        var tt = ParseTriggerType(triggerType);
        return TriggerIconKey.TryGetValue(tt, out var key) ? key : "flash";
    }

    public static string? TryGetFluentSvgUrl(string? iconKey)
    {
        if (string.IsNullOrWhiteSpace(iconKey))
            return null;

        return FluentSvgByIconKey.TryGetValue(iconKey.Trim(), out var url) ? url : null;
    }

    public static string GetFluentSvgUrlOrDefault(string? iconKey, string fallbackIconKey = "doc")
    {
        return TryGetFluentSvgUrl(iconKey)
            ?? TryGetFluentSvgUrl(fallbackIconKey)
            ?? "~/svg/ic_fluent_document_one_page_32_regular.svg";
    }
}

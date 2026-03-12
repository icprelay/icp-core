namespace Icp.Ui;

internal static class ScheduleCronHelpers
{
    internal static string? ApplyWeekDaysToCron(string? baseCron, string[]? selectedDays)
    {
        if (string.IsNullOrWhiteSpace(baseCron))
            return baseCron;

        var days = (selectedDays ?? [])
            .Select(d => (d ?? string.Empty).Trim())
            .Where(d => d.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (days.Length == 0)
            return baseCron;

        var dayTokens = new List<string>();
        foreach (var d in days)
        {
            dayTokens.Add(d.ToLowerInvariant() switch
            {
                "mon" or "monday" => "1",
                "tue" or "tues" or "tuesday" => "2",
                "wed" or "wednesday" => "3",
                "thu" or "thur" or "thurs" or "thursday" => "4",
                "fri" or "friday" => "5",
                "sat" or "saturday" => "6",
                // Use 7 for Sunday (common convention)
                "sun" or "sunday" => "7",
                _ => string.Empty
            });
        }

        dayTokens = dayTokens.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (dayTokens.Count == 0)
            return baseCron;

        // Normalize to 6-part cron: sec min hour dom mon dow
        var parts = baseCron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6)
            return baseCron;

        parts[5] = string.Join(',', dayTokens);
        return string.Join(' ', parts);
    }
}

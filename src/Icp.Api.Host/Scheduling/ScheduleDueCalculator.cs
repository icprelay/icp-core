using Cronos;

namespace Icp.Api.Host.Scheduling;

public static class ScheduleDueCalculator
{
    public static DateTimeOffset? ComputeNextDueAtUtc(string? cron, string? timeZoneId, DateTimeOffset nowUtc)
    {
        if (string.IsNullOrWhiteSpace(cron) || string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        CronExpression expr;
        try
        {
            expr = CronExpression.Parse(NormalizeCronDayOfWeek(cron.Trim()), CronFormat.IncludeSeconds);
        }
        catch
        {
            return null;
        }

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch
        {
            return null;
        }

        // Cronos requires 'fromUtc' to be a UTC DateTime when a timezone is provided.
        var fromUtc = nowUtc.UtcDateTime;

        DateTime? nextUtc;
        try
        {
            nextUtc = expr.GetNextOccurrence(fromUtc, tz, inclusive: false);
        }
        catch
        {
            return null;
        }

        if (nextUtc is null)
            return null;

        return nextUtc;
    }

    private static string NormalizeCronDayOfWeek(string cron)
    {
        // Our UI may emit numeric DOW with Sunday=7. Cronos expects Sunday=0.
        // We only normalize the 6-part format we use: sec min hour dom mon dow
        var parts = (cron ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 6)
            return cron;

        var dow = parts[5];
        if (string.IsNullOrWhiteSpace(dow) || dow == "*" || dow == "?")
            return cron;

        // Replace standalone 7 tokens (and ranges ending/starting with 7) with 0.
        // Keep the transformation conservative: only touch numeric character sequences.
        static string NormalizeToken(string token)
        {
            var t = token.Trim();
            if (t.Length == 0) return t;

            // list items are handled outside; here handle single/range
            if (t.Contains('-', StringComparison.Ordinal))
            {
                var idx = t.IndexOf('-', StringComparison.Ordinal);
                if (idx <= 0 || idx >= t.Length - 1) return t;

                var a = t[..idx];
                var b = t[(idx + 1)..];
                if (a == "7") a = "0";
                if (b == "7") b = "0";
                return $"{a}-{b}";
            }

            return t == "7" ? "0" : t;
        }

        var list = dow.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < list.Length; i++)
            list[i] = NormalizeToken(list[i]);

        parts[5] = string.Join(',', list);
        return string.Join(' ', parts);
    }
}

using System;

namespace TodoTask;

/// <summary>
/// Calcule les instants de reset prévus (toujours en UTC).
/// </summary>
public static class ChecklistResetSchedule
{
    /// <summary>Heure UTC du reset quotidien (15h00).</summary>
    public const int DailyResetHourUtc = 15;

    /// <summary>Heure UTC du reset hebdomadaire (08h00).</summary>
    public const int WeeklyResetHourUtc = 8;

    /// <summary>Jour de la semaine du reset hebdomadaire.</summary>
    public const DayOfWeek WeeklyResetDay = DayOfWeek.Tuesday;

    /// <summary>
    /// Dernier reset quotidien théorique déjà passé par rapport à <paramref name="utcNow"/>.
    /// </summary>
    public static DateTime GetLatestDailyResetUtc(DateTime utcNow)
    {
        var utc = EnsureUtc(utcNow);
        var todayReset = new DateTime(utc.Year, utc.Month, utc.Day, DailyResetHourUtc, 0, 0, DateTimeKind.Utc);
        return utc >= todayReset ? todayReset : todayReset.AddDays(-1);
    }

    /// <summary>
    /// Dernier reset hebdomadaire théorique déjà passé (mardi 08h00 UTC).
    /// </summary>
    public static DateTime GetLatestWeeklyResetUtc(DateTime utcNow)
    {
        var utc = EnsureUtc(utcNow);
        var cursor = new DateTime(utc.Year, utc.Month, utc.Day, WeeklyResetHourUtc, 0, 0, DateTimeKind.Utc);

        var daysSinceWeeklyResetDay = ((int)cursor.DayOfWeek - (int)WeeklyResetDay + 7) % 7;
        cursor = cursor.AddDays(-daysSinceWeeklyResetDay);

        return utc >= cursor ? cursor : cursor.AddDays(-7);
    }

    /// <summary>Interprète une date lue depuis la config comme UTC.</summary>
    public static DateTime? AsUtcOrNull(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return EnsureUtc(value.Value);
    }

    private static DateTime EnsureUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
        };
    }
}

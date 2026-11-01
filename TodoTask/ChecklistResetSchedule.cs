using System;

namespace TodoTask;

// Calcule les instants de reset prévus (toujours en UTC).
public static class ChecklistResetSchedule
{
    // Heure UTC du reset quotidien (15h00).
    public const int DailyResetHourUtc = 15;

    // Heure UTC du reset hebdomadaire (08h00).
    public const int WeeklyResetHourUtc = 8;

    // Jour du reset hebdomadaire.
    public const DayOfWeek WeeklyResetDay = DayOfWeek.Tuesday;

    // Dernier reset quotidien théorique déjà passé à utcNow.
    public static DateTime GetLatestDailyResetUtc(DateTime utcNow)
    {
        var utc = EnsureUtc(utcNow);
        var todayReset = new DateTime(utc.Year, utc.Month, utc.Day, DailyResetHourUtc, 0, 0, DateTimeKind.Utc);
        return utc >= todayReset ? todayReset : todayReset.AddDays(-1);
    }

    // Dernier reset hebdomadaire théorique déjà passé (mardi 08h00 UTC).
    public static DateTime GetLatestWeeklyResetUtc(DateTime utcNow)
    {
        var utc = EnsureUtc(utcNow);
        var cursor = new DateTime(utc.Year, utc.Month, utc.Day, WeeklyResetHourUtc, 0, 0, DateTimeKind.Utc);

        var daysSinceWeeklyResetDay = ((int)cursor.DayOfWeek - (int)WeeklyResetDay + 7) % 7;
        cursor = cursor.AddDays(-daysSinceWeeklyResetDay);

        return utc >= cursor ? cursor : cursor.AddDays(-7);
    }

    // Interprète une date lue depuis la config comme UTC.
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

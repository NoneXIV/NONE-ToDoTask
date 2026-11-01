using System;
using System.Collections.Generic;

namespace TodoTask;

/// <summary>
/// Applique les resets automatiques lorsque l'horaire prévu est dépassé.
/// </summary>
public sealed class ChecklistResetService
{
    /// <summary>
    /// Décoche les sections dont le reset théorique est postérieur au dernier reset enregistré.
    /// </summary>
    /// <returns>Vrai si la configuration a été modifiée (et sauvegardée).</returns>
    public bool ApplyDueResets(Configuration configuration, DateTime utcNow)
    {
        var changed = false;
        var latestDailyReset = ChecklistResetSchedule.GetLatestDailyResetUtc(utcNow);
        var lastDailyReset = ChecklistResetSchedule.AsUtcOrNull(configuration.LastDailyResetAppliedUtc);

        if (lastDailyReset is null || lastDailyReset.Value < latestDailyReset)
        {
            UncheckAll(configuration.DailyCompleted, ChecklistTaskDefinitions.Daily);
            configuration.LastDailyResetAppliedUtc = latestDailyReset;
            changed = true;
        }

        var latestWeeklyReset = ChecklistResetSchedule.GetLatestWeeklyResetUtc(utcNow);
        var lastWeeklyReset = ChecklistResetSchedule.AsUtcOrNull(configuration.LastWeeklyResetAppliedUtc);

        if (lastWeeklyReset is null || lastWeeklyReset.Value < latestWeeklyReset)
        {
            UncheckAll(configuration.WeeklyCompleted, ChecklistTaskDefinitions.Weekly);
            configuration.LastWeeklyResetAppliedUtc = latestWeeklyReset;
            changed = true;
        }

        if (changed)
        {
            configuration.Save();
        }

        return changed;
    }

    /// <summary>
    /// Force le même effet qu'un reset planifié (décoche + dates de frontière à l'instant UTC courant).
    /// Réservé aux tests en plugin dev.
    /// </summary>
    public void SimulateReset(Configuration configuration, DateTime utcNow)
    {
        UncheckAll(configuration.DailyCompleted, ChecklistTaskDefinitions.Daily);
        UncheckAll(configuration.WeeklyCompleted, ChecklistTaskDefinitions.Weekly);
        configuration.LastDailyResetAppliedUtc = ChecklistResetSchedule.GetLatestDailyResetUtc(utcNow);
        configuration.LastWeeklyResetAppliedUtc = ChecklistResetSchedule.GetLatestWeeklyResetUtc(utcNow);
        configuration.Save();
    }

    private static void UncheckAll(
        Dictionary<string, bool> completedByTaskId,
        ChecklistTaskDefinitions.ChecklistTaskDefinition[] tasks)
    {
        foreach (var task in tasks)
        {
            completedByTaskId[task.Id] = false;
        }
    }
}

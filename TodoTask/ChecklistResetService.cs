using System;

namespace TodoTask;

// Applique les resets automatiques quand l'horaire prévu est dépassé.
public sealed class ChecklistResetService
{
    private readonly UserChecklistService checklist;

    public ChecklistResetService(UserChecklistService checklist)
    {
        this.checklist = checklist;
    }

    // Décoche les sections en retard ; retourne vrai si la config a été sauvegardée.
    public bool ApplyDueResets(Configuration configuration, DateTime utcNow)
    {
        var changed = false;
        var latestDailyReset = ChecklistResetSchedule.GetLatestDailyResetUtc(utcNow);
        var lastDailyReset = ChecklistResetSchedule.AsUtcOrNull(configuration.LastDailyResetAppliedUtc);

        if (lastDailyReset is null || lastDailyReset.Value < latestDailyReset)
        {
            checklist.UncheckAllTasks(configuration, TaskResetKind.Quotidien);
            configuration.LastDailyResetAppliedUtc = latestDailyReset;
            changed = true;
        }

        var latestWeeklyReset = ChecklistResetSchedule.GetLatestWeeklyResetUtc(utcNow);
        var lastWeeklyReset = ChecklistResetSchedule.AsUtcOrNull(configuration.LastWeeklyResetAppliedUtc);

        if (lastWeeklyReset is null || lastWeeklyReset.Value < latestWeeklyReset)
        {
            checklist.UncheckAllTasks(configuration, TaskResetKind.Hebdomadaire);
            configuration.LastWeeklyResetAppliedUtc = latestWeeklyReset;
            changed = true;
        }

        if (changed)
        {
            configuration.Save();
        }

        return changed;
    }

    // Même effet qu'un reset planifié, pour les tests en plugin dev.
    public void SimulateReset(Configuration configuration, DateTime utcNow)
    {
        checklist.UncheckAllTasks(configuration, TaskResetKind.Quotidien);
        checklist.UncheckAllTasks(configuration, TaskResetKind.Hebdomadaire);
        configuration.LastDailyResetAppliedUtc = ChecklistResetSchedule.GetLatestDailyResetUtc(utcNow);
        configuration.LastWeeklyResetAppliedUtc = ChecklistResetSchedule.GetLatestWeeklyResetUtc(utcNow);
        configuration.Save();
    }
}

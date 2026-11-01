using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace TodoTask;

// Paramètres du plugin, listes de tâches et état des cases à cocher.
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;

    public bool IsConfigWindowMovable { get; set; } = true;

    // Tâches quotidiennes (ordre d'affichage).
    public List<ChecklistTaskEntry> DailyTasks { get; set; } = [];

    // Tâches hebdomadaires.
    public List<ChecklistTaskEntry> WeeklyTasks { get; set; } = [];

    // Coches quotidiennes, clé = id de tâche.
    public Dictionary<string, bool> DailyCompleted { get; set; } = new();

    // Coches hebdomadaires, clé = id de tâche.
    public Dictionary<string, bool> WeeklyCompleted { get; set; } = new();

    // Dernier reset quotidien appliqué (frontière 15h00 UTC).
    public DateTime? LastDailyResetAppliedUtc { get; set; }

    // Dernier reset hebdomadaire appliqué (mardi 08h00 UTC).
    public DateTime? LastWeeklyResetAppliedUtc { get; set; }

    // Remplit les listes par défaut et les clés de complétion manquantes. Retourne vrai si la config a changé.
    public bool EnsureTaskStateInitialized()
    {
        DailyTasks ??= [];
        WeeklyTasks ??= [];

        var changed = SeedDefaultTasksIfEmpty(DailyTasks, ChecklistTaskDefinitions.DailyDefaultTasks);
        changed |= SeedDefaultTasksIfEmpty(WeeklyTasks, ChecklistTaskDefinitions.WeeklyDefaultTasks);
        changed |= ChecklistTaskEditor.EnsureCompletionState(DailyTasks, DailyCompleted);
        changed |= ChecklistTaskEditor.EnsureCompletionState(WeeklyTasks, WeeklyCompleted);
        return changed;
    }

    // Ajoute une tâche quotidienne et sauvegarde si succès.
    public bool TryAddDailyTask(string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryAddTask(DailyTasks, DailyCompleted, rawLabel, "daily", out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Ajoute une tâche hebdomadaire et sauvegarde si succès.
    public bool TryAddWeeklyTask(string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryAddTask(WeeklyTasks, WeeklyCompleted, rawLabel, "weekly", out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Renomme une tâche quotidienne et sauvegarde si succès.
    public bool TryRenameDailyTask(string taskId, string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRenameTask(DailyTasks, taskId, rawLabel, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Renomme une tâche hebdomadaire et sauvegarde si succès.
    public bool TryRenameWeeklyTask(string taskId, string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRenameTask(WeeklyTasks, taskId, rawLabel, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Supprime une tâche quotidienne et sauvegarde si succès.
    public bool TryRemoveDailyTask(string taskId, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRemoveTask(DailyTasks, DailyCompleted, taskId, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Supprime une tâche hebdomadaire et sauvegarde si succès.
    public bool TryRemoveWeeklyTask(string taskId, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRemoveTask(WeeklyTasks, WeeklyCompleted, taskId, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    // Enregistre la configuration via Dalamud.
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    private static bool SeedDefaultTasksIfEmpty(
        List<ChecklistTaskEntry> tasks,
        ChecklistTaskDefinitions.ChecklistTaskDefinition[] defaults)
    {
        if (tasks.Count > 0)
        {
            return false;
        }

        foreach (var task in defaults)
        {
            tasks.Add(new ChecklistTaskEntry { Id = task.Id, Label = task.Label });
        }

        return true;
    }
}

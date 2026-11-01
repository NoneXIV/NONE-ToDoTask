using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace TodoTask;

/// <summary>
/// Paramètres du plugin, listes de tâches et état des cases à cocher.
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 3;

    public bool IsConfigWindowMovable { get; set; } = true;

    /// <summary>Tâches de la section quotidienne (ordre d'affichage).</summary>
    public List<ChecklistTaskEntry> DailyTasks { get; set; } = [];

    /// <summary>Tâches de la section hebdomadaire.</summary>
    public List<ChecklistTaskEntry> WeeklyTasks { get; set; } = [];

    /// <summary>État coché des tâches quotidiennes, clé = id de tâche.</summary>
    public Dictionary<string, bool> DailyCompleted { get; set; } = new();

    /// <summary>État coché des tâches hebdomadaires, clé = id de tâche.</summary>
    public Dictionary<string, bool> WeeklyCompleted { get; set; } = new();

    /// <summary>Instant UTC du dernier reset quotidien appliqué (frontière 15h00 UTC).</summary>
    public DateTime? LastDailyResetAppliedUtc { get; set; }

    /// <summary>Instant UTC du dernier reset hebdomadaire appliqué (mardi 08h00 UTC).</summary>
    public DateTime? LastWeeklyResetAppliedUtc { get; set; }

    /// <summary>Initialise listes par défaut et clés de complétion manquantes.</summary>
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

    /// <summary>Ajoute une tâche quotidienne et sauvegarde si succès.</summary>
    public bool TryAddDailyTask(string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryAddTask(DailyTasks, DailyCompleted, rawLabel, "daily", out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Ajoute une tâche hebdomadaire et sauvegarde si succès.</summary>
    public bool TryAddWeeklyTask(string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryAddTask(WeeklyTasks, WeeklyCompleted, rawLabel, "weekly", out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Renomme une tâche quotidienne et sauvegarde si succès.</summary>
    public bool TryRenameDailyTask(string taskId, string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRenameTask(DailyTasks, taskId, rawLabel, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Renomme une tâche hebdomadaire et sauvegarde si succès.</summary>
    public bool TryRenameWeeklyTask(string taskId, string rawLabel, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRenameTask(WeeklyTasks, taskId, rawLabel, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Supprime une tâche quotidienne et sauvegarde si succès.</summary>
    public bool TryRemoveDailyTask(string taskId, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRemoveTask(DailyTasks, DailyCompleted, taskId, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Supprime une tâche hebdomadaire et sauvegarde si succès.</summary>
    public bool TryRemoveWeeklyTask(string taskId, out string? errorMessage)
    {
        if (!ChecklistTaskEditor.TryRemoveTask(WeeklyTasks, WeeklyCompleted, taskId, out errorMessage))
        {
            return false;
        }

        Save();
        return true;
    }

    /// <summary>Persiste la configuration via Dalamud.</summary>
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

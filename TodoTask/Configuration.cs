using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace TodoTask;

/// <summary>
/// Paramètres du plugin et état des cases à cocher (quotidien / hebdomadaire).
/// </summary>
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 2;

    public bool IsConfigWindowMovable { get; set; } = true;

    /// <summary>État coché des tâches quotidiennes, clé = <see cref="ChecklistTaskDefinitions.ChecklistTaskDefinition.Id"/>.</summary>
    public Dictionary<string, bool> DailyCompleted { get; set; } = new();

    /// <summary>État coché des tâches hebdomadaires, clé = id de tâche.</summary>
    public Dictionary<string, bool> WeeklyCompleted { get; set; } = new();

    /// <summary>Instant UTC du dernier reset quotidien appliqué (frontière 15h00 UTC).</summary>
    public DateTime? LastDailyResetAppliedUtc { get; set; }

    /// <summary>Instant UTC du dernier reset hebdomadaire appliqué (mardi 08h00 UTC).</summary>
    public DateTime? LastWeeklyResetAppliedUtc { get; set; }

    /// <summary>
    /// Garantit une entrée pour chaque tâche connue (nouvelles tâches du code, config ancienne, etc.).
    /// </summary>
    /// <returns>Vrai si des clés manquantes ont été ajoutées (persiste alors la config).</returns>
    public bool EnsureTaskStateInitialized()
    {
        var changed = EnsureSection(DailyCompleted, ChecklistTaskDefinitions.Daily);
        changed |= EnsureSection(WeeklyCompleted, ChecklistTaskDefinitions.Weekly);
        return changed;
    }

    /// <summary>Persiste la configuration via Dalamud.</summary>
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    private static bool EnsureSection(
        Dictionary<string, bool> completedByTaskId,
        ChecklistTaskDefinitions.ChecklistTaskDefinition[] tasks)
    {
        var changed = false;
        foreach (var task in tasks)
        {
            if (!completedByTaskId.ContainsKey(task.Id))
            {
                completedByTaskId[task.Id] = false;
                changed = true;
            }
        }

        return changed;
    }
}

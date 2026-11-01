using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoTask;

// Paramètres du plugin : sélection catalogue, tâches perso, coches et resets.
[Serializable]
public class Configuration : IPluginConfiguration
{
    private static readonly Dictionary<string, string> LegacyCatalogIdByOldId = new(StringComparer.Ordinal)
    {
        ["daily-roulette"] = "daily.roulettes",
        ["daily-beast-tribe"] = "daily.tribus",
        ["daily-treasure"] = "daily.tresors",
        ["weekly-raid"] = "weekly.raid",
        ["weekly-tome"] = "weekly.tomes",
    };

    public int Version { get; set; } = 4;

    public bool IsConfigWindowMovable { get; set; } = true;

    // Ids des tâches catalogue choisies par l'utilisateur (ordre d'affichage).
    public List<string> SelectedCatalogTaskIds { get; set; } = [];

    // Tâches créées par l'utilisateur.
    public List<PersonalChecklistTask> PersonalTasks { get; set; } = [];

    // Ancien format (v3) : lu uniquement pour migration.
    public List<ChecklistTaskEntry> DailyTasks { get; set; } = [];

    public List<ChecklistTaskEntry> WeeklyTasks { get; set; } = [];

    public Dictionary<string, bool> DailyCompleted { get; set; } = new();

    public Dictionary<string, bool> WeeklyCompleted { get; set; } = new();

    public DateTime? LastDailyResetAppliedUtc { get; set; }

    public DateTime? LastWeeklyResetAppliedUtc { get; set; }

    // Migration, nettoyage et complétion. Retourne vrai si la config a changé.
    public bool EnsureTaskStateInitialized(TaskCatalog catalog, UserChecklistService checklist)
    {
        SelectedCatalogTaskIds ??= [];
        PersonalTasks ??= [];
        DailyTasks ??= [];
        WeeklyTasks ??= [];

        var changed = false;
        if (Version < 4)
        {
            changed |= MigrateFromVersion3(catalog);
            Version = 4;
        }

        changed |= RemoveUnknownCatalogSelections(catalog);
        changed |= SeedDefaultCatalogSelectionIfEmpty(catalog);
        changed |= checklist.EnsureCompletionState(this);
        return changed;
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }

    private bool MigrateFromVersion3(TaskCatalog catalog)
    {
        var changed = false;
        changed |= MigrateLegacyTaskList(DailyTasks, TaskResetKind.Quotidien, catalog);
        changed |= MigrateLegacyTaskList(WeeklyTasks, TaskResetKind.Hebdomadaire, catalog);
        DailyTasks.Clear();
        WeeklyTasks.Clear();
        return changed;
    }

    private bool MigrateLegacyTaskList(
        List<ChecklistTaskEntry> legacyTasks,
        TaskResetKind resetKind,
        TaskCatalog catalog)
    {
        var changed = false;
        foreach (var legacyTask in legacyTasks)
        {
            if (TryResolveCatalogId(legacyTask.Id, catalog, out var catalogId))
            {
                if (!SelectedCatalogTaskIds.Contains(catalogId))
                {
                    SelectedCatalogTaskIds.Add(catalogId);
                    changed = true;
                }

                continue;
            }

            if (PersonalTasks.Any(t => t.Id == legacyTask.Id))
            {
                continue;
            }

            PersonalTasks.Add(new PersonalChecklistTask
            {
                Id = legacyTask.Id.StartsWith("custom-", StringComparison.Ordinal) ? legacyTask.Id : $"custom-{Guid.NewGuid():N}",
                Name = legacyTask.Label,
                Reset = resetKind,
            });
            changed = true;
        }

        return changed;
    }

    private static bool TryResolveCatalogId(string legacyId, TaskCatalog catalog, out string catalogId)
    {
        if (catalog.TryGetTask(legacyId, out _))
        {
            catalogId = legacyId;
            return true;
        }

        if (LegacyCatalogIdByOldId.TryGetValue(legacyId, out var mappedId) && catalog.TryGetTask(mappedId, out _))
        {
            catalogId = mappedId;
            return true;
        }

        catalogId = string.Empty;
        return false;
    }

    private bool RemoveUnknownCatalogSelections(TaskCatalog catalog)
    {
        var changed = false;
        for (var index = SelectedCatalogTaskIds.Count - 1; index >= 0; index--)
        {
            if (!catalog.TryGetTask(SelectedCatalogTaskIds[index], out _))
            {
                SelectedCatalogTaskIds.RemoveAt(index);
                changed = true;
            }
        }

        return changed;
    }

    // Première utilisation : sélectionne les 5 tâches d'exemple du catalogue.
    private bool SeedDefaultCatalogSelectionIfEmpty(TaskCatalog catalog)
    {
        if (SelectedCatalogTaskIds.Count > 0 || PersonalTasks.Count > 0)
        {
            return false;
        }

        foreach (var task in catalog.AllTasks.OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            SelectedCatalogTaskIds.Add(task.Id);
        }

        return SelectedCatalogTaskIds.Count > 0;
    }
}

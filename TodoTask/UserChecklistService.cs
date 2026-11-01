using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoTask;

// Construit la liste affichée et applique les changements utilisateur (catalogue + perso).
public sealed class UserChecklistService
{
    private readonly TaskCatalog catalog;

    public UserChecklistService(TaskCatalog catalog)
    {
        this.catalog = catalog;
    }

    public IReadOnlyList<ChecklistDisplayTask> GetDisplayTasks(Configuration configuration, TaskResetKind resetKind)
    {
        var result = new List<ChecklistDisplayTask>();

        foreach (var catalogTaskId in configuration.SelectedCatalogTaskIds)
        {
            if (!catalog.TryGetTask(catalogTaskId, out var catalogTask) || catalogTask is null)
            {
                continue;
            }

            if (catalogTask.Reset != resetKind)
            {
                continue;
            }

            result.Add(new ChecklistDisplayTask
            {
                Id = catalogTask.Id,
                Label = catalogTask.Nom,
                IsFromCatalog = true,
                Description = catalogTask.Description,
            });
        }

        foreach (var personalTask in configuration.PersonalTasks)
        {
            if (personalTask.Reset != resetKind)
            {
                continue;
            }

            result.Add(new ChecklistDisplayTask
            {
                Id = personalTask.Id,
                Label = personalTask.Name,
                IsFromCatalog = false,
                Description = null,
            });
        }

        return result;
    }

    public IEnumerable<TaskCatalogItem> GetAvailableCatalogTasks(Configuration configuration, TaskResetKind resetKind)
    {
        return catalog
            .GetTasksForReset(resetKind)
            .Where(t => !configuration.SelectedCatalogTaskIds.Contains(t.Id));
    }

    public bool TryAddCatalogTask(Configuration configuration, string catalogTaskId, out string? errorMessage)
    {
        if (!catalog.TryGetTask(catalogTaskId, out var catalogTask) || catalogTask is null)
        {
            errorMessage = "Tâche catalogue introuvable.";
            return false;
        }

        if (configuration.SelectedCatalogTaskIds.Contains(catalogTaskId))
        {
            errorMessage = "Cette tâche est déjà dans votre liste.";
            return false;
        }

        configuration.SelectedCatalogTaskIds.Add(catalogTaskId);
        EnsureCompletionKey(configuration, catalogTaskId, catalogTask.Reset);
        configuration.Save();
        errorMessage = null;
        return true;
    }

    // Ajoute ou retire une tâche catalogue de la liste utilisateur (onglet Catalogue).
    public bool TrySetCatalogTaskInList(
        Configuration configuration,
        string catalogTaskId,
        bool inList,
        out string? errorMessage)
    {
        if (inList)
        {
            return TryAddCatalogTask(configuration, catalogTaskId, out errorMessage);
        }

        if (!configuration.SelectedCatalogTaskIds.Contains(catalogTaskId))
        {
            errorMessage = null;
            return true;
        }

        if (!configuration.SelectedCatalogTaskIds.Remove(catalogTaskId))
        {
            errorMessage = "Tâche catalogue introuvable.";
            return false;
        }

        RemoveCompletionKey(configuration, catalogTaskId);
        configuration.Save();
        errorMessage = null;
        return true;
    }

    public bool IsCatalogTaskInList(Configuration configuration, string catalogTaskId)
    {
        return configuration.SelectedCatalogTaskIds.Contains(catalogTaskId);
    }

    // Tâches du fichier JSON filtrées par recherche, groupées par catégorie.
    public IEnumerable<IGrouping<string, TaskCatalogItem>> GetCatalogTasksGroupedByCategory(string searchQuery)
    {
        IEnumerable<TaskCatalogItem> tasks = catalog.AllTasks;

        var trimmedQuery = searchQuery.Trim();
        if (trimmedQuery.Length > 0)
        {
            tasks = tasks.Where(task => MatchesCatalogSearch(task, trimmedQuery));
        }

        return tasks
            .OrderBy(task => task.Categorie, StringComparer.OrdinalIgnoreCase)
            .ThenBy(task => task.Nom, StringComparer.OrdinalIgnoreCase)
            .GroupBy(task => task.Categorie, StringComparer.OrdinalIgnoreCase);
    }

    private static bool MatchesCatalogSearch(TaskCatalogItem task, string query)
    {
        return task.Nom.Contains(query, StringComparison.OrdinalIgnoreCase)
               || task.Categorie.Contains(query, StringComparison.OrdinalIgnoreCase)
               || task.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
               || task.Id.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryAddPersonalTask(
        Configuration configuration,
        TaskResetKind resetKind,
        string rawLabel,
        out string? errorMessage)
    {
        if (!TryNormalizeLabel(rawLabel, out var label, out errorMessage))
        {
            return false;
        }

        if (HasDuplicateLabel(configuration, resetKind, label, excludeTaskId: null))
        {
            errorMessage = "Une tâche avec ce nom existe déjà.";
            return false;
        }

        var personalTask = new PersonalChecklistTask
        {
            Id = $"custom-{Guid.NewGuid():N}",
            Name = label,
            Reset = resetKind,
        };

        configuration.PersonalTasks.Add(personalTask);
        EnsureCompletionKey(configuration, personalTask.Id, resetKind);
        configuration.Save();
        errorMessage = null;
        return true;
    }

    public bool TryRenamePersonalTask(
        Configuration configuration,
        TaskResetKind resetKind,
        string taskId,
        string rawLabel,
        out string? errorMessage)
    {
        var personalTask = configuration.PersonalTasks.FirstOrDefault(t => t.Id == taskId);
        if (personalTask is null)
        {
            errorMessage = "Seules les tâches perso peuvent être renommées.";
            return false;
        }

        if (!TryNormalizeLabel(rawLabel, out var label, out errorMessage))
        {
            return false;
        }

        if (HasDuplicateLabel(configuration, resetKind, label, excludeTaskId: taskId))
        {
            errorMessage = "Une tâche avec ce nom existe déjà.";
            return false;
        }

        personalTask.Name = label;
        configuration.Save();
        errorMessage = null;
        return true;
    }

    public bool TryRemoveTask(
        Configuration configuration,
        ChecklistDisplayTask displayTask,
        out string? errorMessage)
    {
        if (displayTask.IsFromCatalog)
        {
            if (!configuration.SelectedCatalogTaskIds.Remove(displayTask.Id))
            {
                errorMessage = "Tâche catalogue introuvable.";
                return false;
            }
        }
        else
        {
            var index = configuration.PersonalTasks.FindIndex(t => t.Id == displayTask.Id);
            if (index < 0)
            {
                errorMessage = "Tâche introuvable.";
                return false;
            }

            configuration.PersonalTasks.RemoveAt(index);
        }

        RemoveCompletionKey(configuration, displayTask.Id);
        configuration.Save();
        errorMessage = null;
        return true;
    }

    public void UncheckAllTasks(Configuration configuration, TaskResetKind resetKind)
    {
        foreach (var task in GetDisplayTasks(configuration, resetKind))
        {
            var map = GetCompletedMap(configuration, resetKind);
            map[task.Id] = false;
        }
    }

    public bool EnsureCompletionState(Configuration configuration)
    {
        var changed = false;
        foreach (var resetKind in new[] { TaskResetKind.Quotidien, TaskResetKind.Hebdomadaire })
        {
            foreach (var task in GetDisplayTasks(configuration, resetKind))
            {
                var map = GetCompletedMap(configuration, resetKind);
                if (!map.ContainsKey(task.Id))
                {
                    map[task.Id] = false;
                    changed = true;
                }
            }
        }

        return changed;
    }

    private static Dictionary<string, bool> GetCompletedMap(Configuration configuration, TaskResetKind resetKind)
    {
        return resetKind switch
        {
            TaskResetKind.Quotidien => configuration.DailyCompleted,
            TaskResetKind.Hebdomadaire => configuration.WeeklyCompleted,
            _ => throw new ArgumentOutOfRangeException(nameof(resetKind), resetKind, null),
        };
    }

    private static void EnsureCompletionKey(Configuration configuration, string taskId, TaskResetKind resetKind)
    {
        var map = GetCompletedMap(configuration, resetKind);
        if (!map.ContainsKey(taskId))
        {
            map[taskId] = false;
        }
    }

    private static void RemoveCompletionKey(Configuration configuration, string taskId)
    {
        configuration.DailyCompleted.Remove(taskId);
        configuration.WeeklyCompleted.Remove(taskId);
    }

    private bool HasDuplicateLabel(
        Configuration configuration,
        TaskResetKind resetKind,
        string label,
        string? excludeTaskId)
    {
        foreach (var task in GetDisplayTasks(configuration, resetKind))
        {
            if (task.Id == excludeTaskId)
            {
                continue;
            }

            if (string.Equals(task.Label, label, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Évite une dépendance circulaire : même logique que ChecklistTaskEditor pour les perso.
    private static bool TryNormalizeLabel(string rawLabel, out string label, out string? errorMessage)
    {
        label = rawLabel.Trim();
        if (string.IsNullOrEmpty(label))
        {
            errorMessage = "Le nom ne peut pas être vide.";
            return false;
        }

        if (label.Length > ChecklistTaskEditor.MaxLabelLength)
        {
            errorMessage = $"Le nom ne peut pas dépasser {ChecklistTaskEditor.MaxLabelLength} caractères.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}

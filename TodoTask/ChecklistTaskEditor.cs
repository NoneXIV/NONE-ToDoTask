using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoTask;

/// <summary>Validation et modifications des listes de tâches (quotidien / hebdomadaire).</summary>
public static class ChecklistTaskEditor
{
    /// <summary>Longueur maximale d'un libellé de tâche.</summary>
    public const int MaxLabelLength = 120;

    /// <summary>Ajoute une tâche si le libellé est valide et unique dans la section.</summary>
    public static bool TryAddTask(
        List<ChecklistTaskEntry> tasks,
        Dictionary<string, bool> completedByTaskId,
        string rawLabel,
        string idPrefix,
        out string? errorMessage)
    {
        if (!TryNormalizeLabel(rawLabel, out var label, out errorMessage))
        {
            return false;
        }

        if (HasDuplicateLabel(tasks, label, excludeTaskId: null))
        {
            errorMessage = "Une tâche avec ce nom existe déjà.";
            return false;
        }

        var entry = new ChecklistTaskEntry
        {
            Id = $"{idPrefix}-{Guid.NewGuid():N}",
            Label = label,
        };

        tasks.Add(entry);
        completedByTaskId[entry.Id] = false;
        errorMessage = null;
        return true;
    }

    /// <summary>Renomme une tâche existante.</summary>
    public static bool TryRenameTask(
        List<ChecklistTaskEntry> tasks,
        string taskId,
        string rawLabel,
        out string? errorMessage)
    {
        var entry = tasks.FirstOrDefault(t => t.Id == taskId);
        if (entry is null)
        {
            errorMessage = "Tâche introuvable.";
            return false;
        }

        if (!TryNormalizeLabel(rawLabel, out var label, out errorMessage))
        {
            return false;
        }

        if (HasDuplicateLabel(tasks, label, excludeTaskId: taskId))
        {
            errorMessage = "Une tâche avec ce nom existe déjà.";
            return false;
        }

        entry.Label = label;
        errorMessage = null;
        return true;
    }

    /// <summary>Supprime une tâche et son état coché.</summary>
    public static bool TryRemoveTask(
        List<ChecklistTaskEntry> tasks,
        Dictionary<string, bool> completedByTaskId,
        string taskId,
        out string? errorMessage)
    {
        var index = tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
        {
            errorMessage = "Tâche introuvable.";
            return false;
        }

        tasks.RemoveAt(index);
        completedByTaskId.Remove(taskId);
        errorMessage = null;
        return true;
    }

    /// <summary>Décoche toutes les tâches d'une section sans modifier la liste.</summary>
    public static void UncheckAllTasks(
        IReadOnlyList<ChecklistTaskEntry> tasks,
        Dictionary<string, bool> completedByTaskId)
    {
        foreach (var task in tasks)
        {
            completedByTaskId[task.Id] = false;
        }
    }

    /// <summary>Garantit une entrée cochée/non cochée pour chaque tâche de la liste.</summary>
    public static bool EnsureCompletionState(
        IReadOnlyList<ChecklistTaskEntry> tasks,
        Dictionary<string, bool> completedByTaskId)
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

    private static bool TryNormalizeLabel(string rawLabel, out string label, out string? errorMessage)
    {
        label = rawLabel.Trim();
        if (string.IsNullOrEmpty(label))
        {
            errorMessage = "Le nom ne peut pas être vide.";
            return false;
        }

        if (label.Length > MaxLabelLength)
        {
            errorMessage = $"Le nom ne peut pas dépasser {MaxLabelLength} caractères.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool HasDuplicateLabel(
        IReadOnlyList<ChecklistTaskEntry> tasks,
        string label,
        string? excludeTaskId)
    {
        return tasks.Any(t => t.Id != excludeTaskId
                              && string.Equals(t.Label, label, StringComparison.OrdinalIgnoreCase));
    }
}

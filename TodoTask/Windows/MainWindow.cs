using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace TodoTask.Windows;

// Fenêtre principale : checklist quotidienne et hebdomadaire.
public class MainWindow : Window, IDisposable
{
    private const string SectionDailyTitle = "Quotidien";
    private const string SectionWeeklyTitle = "Hebdomadaire";
    private const string AddPersonalTaskButtonLabel = "Ajouter";
    private const string AddCatalogTaskButtonLabel = "Ajouter du catalogue";
    private const string DeleteTaskButtonLabel = "X";
    private const string SimulateResetButtonLabel = "Simuler un reset";
    private const string ConfirmDeletePopupId = "TodoTaskConfirmDelete###TodoTaskConfirmDelete";
    private const string ConfirmDeleteYesLabel = "Supprimer";
    private const string ConfirmDeleteNoLabel = "Annuler";

    private readonly Plugin plugin;
    private readonly Configuration configuration;
    private readonly UserChecklistService checklist;

    private string dailyPersonalTaskInput = string.Empty;
    private string weeklyPersonalTaskInput = string.Empty;
    private string? dailyAddErrorMessage;
    private string? weeklyAddErrorMessage;
    private string? dailyRenameErrorMessage;
    private string? weeklyRenameErrorMessage;
    private int dailyCatalogPickerIndex;
    private int weeklyCatalogPickerIndex;

    private ChecklistDisplayTask? pendingDeleteTask;
    private string pendingDeleteTaskLabel = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Todo Task##TodoTaskMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(400, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
        configuration = plugin.Configuration;
        checklist = plugin.UserChecklist;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var scrollArea = ImRaii.Child("ChecklistScroll", Vector2.Zero, true);
        if (!scrollArea.Success)
        {
            return;
        }

        DrawTaskSection(TaskResetKind.Quotidien, SectionDailyTitle);
        ImGuiHelpers.ScaledDummy(12f);
        DrawTaskSection(TaskResetKind.Hebdomadaire, SectionWeeklyTitle);

        if (plugin.IsDevelopmentPlugin)
        {
            ImGuiHelpers.ScaledDummy(12f);
            ImGui.Separator();
            if (ImGui.Button(SimulateResetButtonLabel))
            {
                plugin.SimulateScheduledReset();
            }
        }

        DrawDeleteConfirmationPopup();
    }

    private void DrawTaskSection(TaskResetKind resetKind, string sectionTitle)
    {
        var tasks = checklist.GetDisplayTasks(configuration, resetKind);
        var completedByTaskId = GetCompletedMap(resetKind);

        ImGui.Text(sectionTitle);
        ImGui.Separator();
        ImGui.Spacing();

        DrawCatalogAddRow(resetKind);
        DrawPersonalAddRow(resetKind);

        foreach (var task in tasks)
        {
            DrawTaskRow(resetKind, task, completedByTaskId);
        }

        var renameError = GetRenameErrorMessage(resetKind);
        if (!string.IsNullOrEmpty(renameError))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), renameError);
        }
    }

    private void DrawCatalogAddRow(TaskResetKind resetKind)
    {
        var availableTasks = checklist.GetAvailableCatalogTasks(configuration, resetKind).ToList();
        if (availableTasks.Count == 0)
        {
            return;
        }

        ref var pickerIndex = ref GetCatalogPickerIndex(resetKind);
        if (pickerIndex >= availableTasks.Count)
        {
            pickerIndex = 0;
        }

        var preview = availableTasks[pickerIndex].Nom;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.55f);
        if (ImGui.BeginCombo($"##CatalogPicker{resetKind}", preview))
        {
            for (var index = 0; index < availableTasks.Count; index++)
            {
                var task = availableTasks[index];
                if (ImGui.Selectable($"{task.Nom} ({task.Categorie})##{task.Id}", index == pickerIndex))
                {
                    pickerIndex = index;
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button($"{AddCatalogTaskButtonLabel}##{resetKind}"))
        {
            ref var addError = ref GetAddErrorMessage(resetKind);
            if (checklist.TryAddCatalogTask(configuration, availableTasks[pickerIndex].Id, out var errorMessage))
            {
                addError = null;
                pickerIndex = 0;
            }
            else
            {
                addError = errorMessage;
            }
        }

        ImGui.Spacing();
    }

    private void DrawPersonalAddRow(TaskResetKind resetKind)
    {
        ref var input = ref GetPersonalTaskInput(resetKind);
        ref var addError = ref GetAddErrorMessage(resetKind);

        var addButtonWidth = ImGui.CalcTextSize(AddPersonalTaskButtonLabel).X + (ImGui.GetStyle().FramePadding.X * 2f);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - addButtonWidth - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputTextWithHint($"##PersonalTask{resetKind}", "Tâche perso…", ref input, ChecklistTaskEditor.MaxLabelLength);

        ImGui.SameLine();
        if (ImGui.Button($"{AddPersonalTaskButtonLabel}##personal_{resetKind}"))
        {
            if (checklist.TryAddPersonalTask(configuration, resetKind, input, out var errorMessage))
            {
                input = string.Empty;
                addError = null;
            }
            else
            {
                addError = errorMessage;
            }
        }

        if (!string.IsNullOrEmpty(addError))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), addError);
        }

        ImGui.Spacing();
    }

    private void DrawTaskRow(
        TaskResetKind resetKind,
        ChecklistDisplayTask task,
        Dictionary<string, bool> completedByTaskId)
    {
        var isCompleted = completedByTaskId[task.Id];
        if (ImGui.Checkbox($"##done_{task.Id}", ref isCompleted))
        {
            completedByTaskId[task.Id] = isCompleted;
            configuration.Save();
        }

        ImGui.SameLine();

        var deleteButtonWidth = ImGui.CalcTextSize(DeleteTaskButtonLabel).X + (ImGui.GetStyle().FramePadding.X * 2f);
        var labelWidth = ImGui.GetContentRegionAvail().X - deleteButtonWidth - ImGui.GetStyle().ItemSpacing.X;

        if (task.CanRename)
        {
            var labelBuffer = task.Label;
            ImGui.SetNextItemWidth(labelWidth);
            ImGui.InputText($"##label_{task.Id}", ref labelBuffer, ChecklistTaskEditor.MaxLabelLength);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                if (checklist.TryRenamePersonalTask(configuration, resetKind, task.Id, labelBuffer, out var errorMessage))
                {
                    SetRenameErrorMessage(resetKind, null);
                }
                else
                {
                    SetRenameErrorMessage(resetKind, errorMessage);
                }
            }
        }
        else
        {
            ImGui.SetNextItemWidth(labelWidth);
            ImGui.TextUnformatted(task.Label);
            if (!string.IsNullOrWhiteSpace(task.Description) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(task.Description);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button($"{DeleteTaskButtonLabel}##del_{task.Id}"))
        {
            pendingDeleteTask = task;
            pendingDeleteTaskLabel = task.Label;
            ImGui.OpenPopup(ConfirmDeletePopupId);
        }
    }

    private void DrawDeleteConfirmationPopup()
    {
        if (!ImGui.BeginPopupModal(ConfirmDeletePopupId, ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        ImGui.Text($"Supprimer « {pendingDeleteTaskLabel} » ?");

        if (ImGui.Button(ConfirmDeleteYesLabel))
        {
            if (pendingDeleteTask is not null)
            {
                checklist.TryRemoveTask(configuration, pendingDeleteTask, out _);
            }

            pendingDeleteTask = null;
            pendingDeleteTaskLabel = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button(ConfirmDeleteNoLabel))
        {
            pendingDeleteTask = null;
            pendingDeleteTaskLabel = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private Dictionary<string, bool> GetCompletedMap(TaskResetKind resetKind)
    {
        return resetKind switch
        {
            TaskResetKind.Quotidien => configuration.DailyCompleted,
            TaskResetKind.Hebdomadaire => configuration.WeeklyCompleted,
            _ => throw new ArgumentOutOfRangeException(nameof(resetKind), resetKind, null),
        };
    }

    private ref string GetPersonalTaskInput(TaskResetKind resetKind)
    {
        if (resetKind == TaskResetKind.Quotidien)
        {
            return ref dailyPersonalTaskInput;
        }

        return ref weeklyPersonalTaskInput;
    }

    private ref string? GetAddErrorMessage(TaskResetKind resetKind)
    {
        if (resetKind == TaskResetKind.Quotidien)
        {
            return ref dailyAddErrorMessage;
        }

        return ref weeklyAddErrorMessage;
    }

    private ref int GetCatalogPickerIndex(TaskResetKind resetKind)
    {
        if (resetKind == TaskResetKind.Quotidien)
        {
            return ref dailyCatalogPickerIndex;
        }

        return ref weeklyCatalogPickerIndex;
    }

    private string? GetRenameErrorMessage(TaskResetKind resetKind)
    {
        return resetKind switch
        {
            TaskResetKind.Quotidien => dailyRenameErrorMessage,
            TaskResetKind.Hebdomadaire => weeklyRenameErrorMessage,
            _ => null,
        };
    }

    private void SetRenameErrorMessage(TaskResetKind resetKind, string? message)
    {
        switch (resetKind)
        {
            case TaskResetKind.Quotidien:
                dailyRenameErrorMessage = message;
                break;
            case TaskResetKind.Hebdomadaire:
                weeklyRenameErrorMessage = message;
                break;
        }
    }
}

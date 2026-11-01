using System;
using System.Collections.Generic;
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
    private const string AddTaskButtonLabel = "Ajouter";
    private const string DeleteTaskButtonLabel = "X";
    private const string SimulateResetButtonLabel = "Simuler un reset";
    private const string ConfirmDeletePopupId = "TodoTaskConfirmDelete###TodoTaskConfirmDelete";
    private const string ConfirmDeleteYesLabel = "Supprimer";
    private const string ConfirmDeleteNoLabel = "Annuler";

    private readonly Plugin plugin;
    private readonly Configuration configuration;

    private string dailyNewTaskInput = string.Empty;
    private string weeklyNewTaskInput = string.Empty;
    private string? dailyAddErrorMessage;
    private string? weeklyAddErrorMessage;
    private string? dailyRenameErrorMessage;
    private string? weeklyRenameErrorMessage;

    private ChecklistSectionKind? pendingDeleteSection;
    private string? pendingDeleteTaskId;
    private string pendingDeleteTaskLabel = string.Empty;

    public MainWindow(Plugin plugin)
        : base("Todo Task##TodoTaskMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(360, 280),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        using var scrollArea = ImRaii.Child("ChecklistScroll", Vector2.Zero, true);
        if (!scrollArea.Success)
        {
            return;
        }

        DrawTaskSection(ChecklistSectionKind.Daily, SectionDailyTitle);
        ImGuiHelpers.ScaledDummy(12f);
        DrawTaskSection(ChecklistSectionKind.Weekly, SectionWeeklyTitle);

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

    private void DrawTaskSection(ChecklistSectionKind section, string sectionTitle)
    {
        var tasks = GetTasks(section);
        var completedByTaskId = GetCompletedMap(section);

        ImGui.Text(sectionTitle);
        ImGui.Separator();
        ImGui.Spacing();

        DrawAddTaskRow(section);

        foreach (var task in tasks)
        {
            DrawTaskRow(section, task, completedByTaskId);
        }

        if (!string.IsNullOrEmpty(GetRenameErrorMessage(section)))
        {
            ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), GetRenameErrorMessage(section));
        }
    }

    private string? GetRenameErrorMessage(ChecklistSectionKind section)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => dailyRenameErrorMessage,
            ChecklistSectionKind.Weekly => weeklyRenameErrorMessage,
            _ => null,
        };
    }

    private void SetRenameErrorMessage(ChecklistSectionKind section, string? message)
    {
        switch (section)
        {
            case ChecklistSectionKind.Daily:
                dailyRenameErrorMessage = message;
                break;
            case ChecklistSectionKind.Weekly:
                weeklyRenameErrorMessage = message;
                break;
        }
    }

    private void DrawAddTaskRow(ChecklistSectionKind section)
    {
        if (section == ChecklistSectionKind.Daily)
        {
            DrawAddTaskRowCore(section, ref dailyNewTaskInput, ref dailyAddErrorMessage);
        }
        else
        {
            DrawAddTaskRowCore(section, ref weeklyNewTaskInput, ref weeklyAddErrorMessage);
        }
    }

    private void DrawAddTaskRowCore(ChecklistSectionKind section, ref string input, ref string? addError)
    {
        var addButtonWidth = ImGui.CalcTextSize(AddTaskButtonLabel).X + (ImGui.GetStyle().FramePadding.X * 2f);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - addButtonWidth - ImGui.GetStyle().ItemSpacing.X);
        ImGui.InputTextWithHint($"##NewTask{section}", "Nouvelle tâche…", ref input, ChecklistTaskEditor.MaxLabelLength);

        ImGui.SameLine();
        if (ImGui.Button($"{AddTaskButtonLabel}##{section}"))
        {
            if (TryAddTask(section, input, out var errorMessage))
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
        ChecklistSectionKind section,
        ChecklistTaskEntry task,
        Dictionary<string, bool> completedByTaskId)
    {
        var isCompleted = completedByTaskId[task.Id];
        if (ImGui.Checkbox($"##done_{task.Id}", ref isCompleted))
        {
            completedByTaskId[task.Id] = isCompleted;
            configuration.Save();
        }

        ImGui.SameLine();

        var labelBuffer = task.Label;
        var deleteButtonWidth = ImGui.CalcTextSize(DeleteTaskButtonLabel).X + (ImGui.GetStyle().FramePadding.X * 2f);
        var labelWidth = ImGui.GetContentRegionAvail().X - deleteButtonWidth - ImGui.GetStyle().ItemSpacing.X;
        ImGui.SetNextItemWidth(labelWidth);
        ImGui.InputText($"##label_{task.Id}", ref labelBuffer, ChecklistTaskEditor.MaxLabelLength);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            if (TryRenameTask(section, task.Id, labelBuffer, out var errorMessage))
            {
                SetRenameErrorMessage(section, null);
            }
            else
            {
                SetRenameErrorMessage(section, errorMessage);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button($"{DeleteTaskButtonLabel}##del_{task.Id}"))
        {
            pendingDeleteSection = section;
            pendingDeleteTaskId = task.Id;
            pendingDeleteTaskLabel = task.Label;
            ImGui.OpenPopup(ConfirmDeletePopupId);
        }
    }

    private void DrawDeleteConfirmationPopup()
    {
        var isOpen = ImGui.BeginPopupModal(ConfirmDeletePopupId, ImGuiWindowFlags.AlwaysAutoResize);
        if (!isOpen)
        {
            return;
        }

        ImGui.Text($"Supprimer « {pendingDeleteTaskLabel} » ?");

        if (ImGui.Button(ConfirmDeleteYesLabel))
        {
            if (pendingDeleteSection is { } section && pendingDeleteTaskId is not null)
            {
                TryRemoveTask(section, pendingDeleteTaskId, out _);
            }

            pendingDeleteSection = null;
            pendingDeleteTaskId = null;
            pendingDeleteTaskLabel = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (ImGui.Button(ConfirmDeleteNoLabel))
        {
            pendingDeleteSection = null;
            pendingDeleteTaskId = null;
            pendingDeleteTaskLabel = string.Empty;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private List<ChecklistTaskEntry> GetTasks(ChecklistSectionKind section)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => configuration.DailyTasks,
            ChecklistSectionKind.Weekly => configuration.WeeklyTasks,
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
        };
    }

    private Dictionary<string, bool> GetCompletedMap(ChecklistSectionKind section)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => configuration.DailyCompleted,
            ChecklistSectionKind.Weekly => configuration.WeeklyCompleted,
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
        };
    }

    private bool TryAddTask(ChecklistSectionKind section, string rawLabel, out string? errorMessage)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => configuration.TryAddDailyTask(rawLabel, out errorMessage),
            ChecklistSectionKind.Weekly => configuration.TryAddWeeklyTask(rawLabel, out errorMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
        };
    }

    private bool TryRenameTask(ChecklistSectionKind section, string taskId, string rawLabel, out string? errorMessage)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => configuration.TryRenameDailyTask(taskId, rawLabel, out errorMessage),
            ChecklistSectionKind.Weekly => configuration.TryRenameWeeklyTask(taskId, rawLabel, out errorMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
        };
    }

    private bool TryRemoveTask(ChecklistSectionKind section, string taskId, out string? errorMessage)
    {
        return section switch
        {
            ChecklistSectionKind.Daily => configuration.TryRemoveDailyTask(taskId, out errorMessage),
            ChecklistSectionKind.Weekly => configuration.TryRemoveWeeklyTask(taskId, out errorMessage),
            _ => throw new ArgumentOutOfRangeException(nameof(section), section, null),
        };
    }

    private enum ChecklistSectionKind
    {
        Daily,
        Weekly,
    }
}

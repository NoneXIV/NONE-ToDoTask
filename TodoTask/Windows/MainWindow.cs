using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace TodoTask.Windows;

/// <summary>Fenêtre principale : checklist quotidienne et hebdomadaire.</summary>
public class MainWindow : Window, IDisposable
{
    private const string SectionDailyTitle = "Quotidien";
    private const string SectionWeeklyTitle = "Hebdomadaire";

    private readonly Configuration configuration;

    public MainWindow(Plugin plugin)
        : base("Todo Task##TodoTaskMainWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

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

        DrawTaskSection(SectionDailyTitle, ChecklistTaskDefinitions.Daily, configuration.DailyCompleted);
        ImGuiHelpers.ScaledDummy(12f);
        DrawTaskSection(SectionWeeklyTitle, ChecklistTaskDefinitions.Weekly, configuration.WeeklyCompleted);
    }

    /// <summary>Affiche un titre de section et une case par tâche.</summary>
    private void DrawTaskSection(
        string sectionTitle,
        ChecklistTaskDefinitions.ChecklistTaskDefinition[] tasks,
        Dictionary<string, bool> completedByTaskId)
    {
        ImGui.Text(sectionTitle);
        ImGui.Separator();
        ImGui.Spacing();

        foreach (var task in tasks)
        {
            var isCompleted = completedByTaskId[task.Id];
            if (ImGui.Checkbox($"{task.Label}##{task.Id}", ref isCompleted))
            {
                completedByTaskId[task.Id] = isCompleted;
                configuration.Save();
            }
        }
    }
}

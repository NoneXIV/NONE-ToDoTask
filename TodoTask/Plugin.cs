using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using TodoTask.Windows;

namespace TodoTask;

/// <summary>Point d'entrée du plugin Dalamud Todo Task.</summary>
public sealed class Plugin : IDalamudPlugin
{
    /// <summary>Intervalle entre deux vérifications de reset pendant que le jeu tourne.</summary>
    private const int ResetCheckIntervalSeconds = 30;

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/todotask";

    private readonly ChecklistResetService resetService = new();
    private DateTime nextResetCheckUtc = DateTime.MinValue;

    /// <summary>Vrai si le plugin est chargé depuis un emplacement dev Dalamud.</summary>
    public bool IsDevelopmentPlugin => PluginInterface.IsDev;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("TodoTask");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (Configuration.EnsureTaskStateInitialized())
        {
            Configuration.Save();
        }

        ApplyDueResetsUtc(DateTime.UtcNow);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Ouvre ou ferme la checklist Todo Task.",
        });

        Framework.Update += OnFrameworkUpdate;

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Log.Information($"{PluginInterface.Manifest.Name} chargé.");
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            var utcNow = DateTime.UtcNow;
            if (utcNow < nextResetCheckUtc)
            {
                return;
            }

            nextResetCheckUtc = utcNow.AddSeconds(ResetCheckIntervalSeconds);
            ApplyDueResetsUtc(utcNow);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Échec de la vérification des resets automatiques.");
        }
    }

    private void ApplyDueResetsUtc(DateTime utcNow)
    {
        if (resetService.ApplyDueResets(Configuration, utcNow))
        {
            Log.Debug(
                $"Reset checklist appliqué (UTC {utcNow:O}, quotidien jusqu'à {Configuration.LastDailyResetAppliedUtc:O}, hebdo jusqu'à {Configuration.LastWeeklyResetAppliedUtc:O}).");
        }
    }

    /// <summary>Déclenche manuellement un reset (visible uniquement en plugin dev).</summary>
    public void SimulateScheduledReset()
    {
        var utcNow = DateTime.UtcNow;
        resetService.SimulateReset(Configuration, utcNow);
        Log.Information($"Reset simulé (dev, UTC {utcNow:O}).");
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}

using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoTask;

// Charge et expose le catalogue de tâches depuis Data/task-catalog.json.
public sealed class TaskCatalog
{
    private const string CatalogFileName = "task-catalog.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Dictionary<string, TaskCatalogItem> tasksById;

    public TaskCatalog(IReadOnlyDictionary<string, TaskCatalogItem> tasksById)
    {
        this.tasksById = new Dictionary<string, TaskCatalogItem>(tasksById, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<TaskCatalogItem> AllTasks => tasksById.Values;

    public bool TryGetTask(string taskId, out TaskCatalogItem? task)
    {
        if (tasksById.TryGetValue(taskId, out var found))
        {
            task = found;
            return true;
        }

        task = null;
        return false;
    }

    public IEnumerable<TaskCatalogItem> GetTasksForReset(TaskResetKind resetKind)
    {
        return tasksById.Values.Where(t => t.Reset == resetKind);
    }

    // Lit le catalogue à côté de la DLL du plugin (copié depuis Data/ à la compilation).
    public static TaskCatalog LoadFromPluginDirectory(string pluginDirectory, IPluginLog log)
    {
        var catalogPath = Path.Combine(pluginDirectory, CatalogFileName);
        try
        {
            if (!File.Exists(catalogPath))
            {
                log.Error($"Catalogue introuvable : {catalogPath}");
                return new TaskCatalog(new Dictionary<string, TaskCatalogItem>());
            }

            var json = File.ReadAllText(catalogPath);
            var document = JsonSerializer.Deserialize<TaskCatalogDocument>(json, JsonOptions);
            if (document?.Tasks is null || document.Tasks.Count == 0)
            {
                log.Error("Le fichier catalogue ne contient aucune tâche.");
                return new TaskCatalog(new Dictionary<string, TaskCatalogItem>());
            }

            var map = new Dictionary<string, TaskCatalogItem>(StringComparer.Ordinal);
            foreach (var task in document.Tasks)
            {
                if (string.IsNullOrWhiteSpace(task.Id))
                {
                    log.Warning("Entrée catalogue ignorée : id manquant.");
                    continue;
                }

                if (map.ContainsKey(task.Id))
                {
                    log.Warning($"Entrée catalogue ignorée : id en double « {task.Id} ».");
                    continue;
                }

                map[task.Id] = task;
            }

            log.Information($"Catalogue chargé : {map.Count} tâche(s).");
            return new TaskCatalog(map);
        }
        catch (Exception ex)
        {
            log.Error(ex, "Impossible de charger le catalogue de tâches.");
            return new TaskCatalog(new Dictionary<string, TaskCatalogItem>());
        }
    }

    private sealed class TaskCatalogDocument
    {
        public List<TaskCatalogItem> Tasks { get; set; } = [];
    }
}

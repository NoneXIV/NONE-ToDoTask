namespace TodoTask;

// Entrée du fichier JSON task-catalog.json.
public sealed class TaskCatalogItem
{
    public string Id { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public string Categorie { get; set; } = string.Empty;

    public TaskResetKind Reset { get; set; }

    public string Description { get; set; } = string.Empty;
}

namespace TodoTask;

// Tâche affichée dans la checklist (catalogue ou perso).
public sealed class ChecklistDisplayTask
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public required bool IsFromCatalog { get; init; }

    public string? Description { get; init; }

    public bool CanRename => !IsFromCatalog;
}

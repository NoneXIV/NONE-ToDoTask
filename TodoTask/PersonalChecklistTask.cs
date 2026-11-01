using System;

namespace TodoTask;

// Tâche personnalisée créée par l'utilisateur (nom et reset stockés en config).
[Serializable]
public sealed class PersonalChecklistTask
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TaskResetKind Reset { get; set; }
}

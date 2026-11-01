using System;

namespace TodoTask;

// Tâche persistée dans la configuration (id stable, libellé modifiable).
[Serializable]
public sealed class ChecklistTaskEntry
{
    // Identifiant unique (inchangé au renommage).
    public string Id { get; set; } = string.Empty;

    // Libellé affiché dans l'interface.
    public string Label { get; set; } = string.Empty;
}

using System;

namespace TodoTask;

/// <summary>Une tâche persistée dans la configuration (id stable, libellé modifiable).</summary>
[Serializable]
public sealed class ChecklistTaskEntry
{
    /// <summary>Identifiant unique de la tâche (ne change pas au renommage).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Libellé affiché dans l'interface.</summary>
    public string Label { get; set; } = string.Empty;
}

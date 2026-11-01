namespace TodoTask;

/// <summary>
/// Liste fixe des tâches affichées à l'étape 1 (pas d'ajout dynamique pour l'instant).
/// </summary>
public static class ChecklistTaskDefinitions
{
    /// <summary>Identifiant stable d'une entrée de checklist (persisté dans la configuration).</summary>
    public readonly record struct ChecklistTaskDefinition(string Id, string Label);

    /// <summary>Tâches de la section quotidienne.</summary>
    public static readonly ChecklistTaskDefinition[] Daily =
    [
        new("daily-roulette", "Roulettes"),
        new("daily-beast-tribe", "Quêtes tribus"),
        new("daily-treasure", "Cartes aux trésors"),
    ];

    /// <summary>Tâches de la section hebdomadaire.</summary>
    public static readonly ChecklistTaskDefinition[] Weekly =
    [
        new("weekly-raid", "Raid de la semaine"),
        new("weekly-tome", "Cap tomes"),
        new("weekly-custom", "Objectif perso"),
    ];
}

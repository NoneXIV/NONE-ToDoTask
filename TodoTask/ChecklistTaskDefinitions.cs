namespace TodoTask;

/// <summary>Tâches par défaut utilisées lors de la première initialisation de la configuration.</summary>
public static class ChecklistTaskDefinitions
{
    /// <summary>Identifiant stable et libellé d'une tâche modèle.</summary>
    public readonly record struct ChecklistTaskDefinition(string Id, string Label);

    /// <summary>Tâches quotidiennes initiales.</summary>
    public static readonly ChecklistTaskDefinition[] DailyDefaultTasks =
    [
        new("daily-roulette", "Roulettes"),
        new("daily-beast-tribe", "Quêtes tribus"),
        new("daily-treasure", "Cartes aux trésors"),
    ];

    /// <summary>Tâches hebdomadaires initiales.</summary>
    public static readonly ChecklistTaskDefinition[] WeeklyDefaultTasks =
    [
        new("weekly-raid", "Raid de la semaine"),
        new("weekly-tome", "Cap tomes"),
        new("weekly-custom", "Objectif perso"),
    ];
}

namespace TodoTask;

// Tâches par défaut au premier lancement de la configuration.
public static class ChecklistTaskDefinitions
{
    // Id stable et libellé d'une tâche modèle.
    public readonly record struct ChecklistTaskDefinition(string Id, string Label);

    // Tâches quotidiennes initiales.
    public static readonly ChecklistTaskDefinition[] DailyDefaultTasks =
    [
        new("daily-roulette", "Roulettes"),
        new("daily-beast-tribe", "Quêtes tribus"),
        new("daily-treasure", "Cartes aux trésors"),
    ];

    // Tâches hebdomadaires initiales.
    public static readonly ChecklistTaskDefinition[] WeeklyDefaultTasks =
    [
        new("weekly-raid", "Raid de la semaine"),
        new("weekly-tome", "Cap tomes"),
        new("weekly-custom", "Objectif perso"),
    ];
}

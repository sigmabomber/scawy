using Doody.Framework.ObjectiveSystem;
using Doody.GameEvents;
using System.Collections.Generic;

public class ObjectiveManager : EventListener
{
    private static ObjectiveManager instance;
    public static ObjectiveManager Instance => instance;

    private Dictionary<string, Objective> objectives = new Dictionary<string, Objective>();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to game events
        Listen<EnemyKilledEvent>(OnEnemyKilled);
        Listen<ItemCollectedEvent>(OnItemCollected);
        Listen<ObjectiveProgressEvent>(OnObjectiveProgress);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        // Check which objectives care about this enemy type
        var objective = GetObjective("kill_enemies");
        objective?.AddProgress(e.Count);

        // Or if tracking specific enemy types:
        var specificObjective = GetObjective($"kill_{e.EnemyType}");
        specificObjective?.AddProgress(e.Count);
    }

    private void OnItemCollected(ItemCollectedEvent e)
    {
        var objective = GetObjective("collect_items");
        if (objective is CollectionObjective collectionObj)
        {
            collectionObj.CollectItem(e.ItemId);
        }
    }

    private void OnObjectiveProgress(ObjectiveProgressEvent e)
    {
        var objective = GetObjective(e.ObjectiveId);
        objective?.AddProgress(e.Amount);
    }

    // Your existing ObjectiveManager code...
    public void AddObjective(Objective objective) { /* ... */ }
    public Objective GetObjective(string id)
    {
        return null;
    }
}
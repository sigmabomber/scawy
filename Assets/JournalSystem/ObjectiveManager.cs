using Doody.Framework.ObjectiveSystem;
using Doody.GameEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectiveManager : EventListener
{
    private static ObjectiveManager instance;
    public static ObjectiveManager Instance => instance;

    private Dictionary<string, Objective> objectives = new Dictionary<string, Objective>();

    public event Action<Objective> OnObjectiveCompleted;
    public event Action<Objective> OnObjectiveAdded;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Listen<CountObjective>(obj => AddObjective(obj));
        Listen<CollectionObjective>(obj => AddObjective(obj));
        Listen<BooleanObjective>(obj => AddObjective(obj));

        Listen<EnemyKilledEvent>(OnEnemyKilled);
        Listen<ItemCollectedEvent>(OnItemCollected);
        Listen<ObjectiveProgressEvent>(OnObjectiveProgress);

    }

    public void AddObjective(Objective objective)
    {
        if (objectives.ContainsKey(objective.Id))
        {
            Debug.LogWarning($"Objective with ID '{objective.Id}' already exists!");
            return;
        }

        objectives[objective.Id] = objective;
        objective.OnCompleted += HandleObjectiveCompleted;
        OnObjectiveAdded?.Invoke(objective);

    }

    public void RemoveObjective(string id)
    {
        if (objectives.TryGetValue(id, out var objective))
        {
            objective.OnCompleted -= HandleObjectiveCompleted;
            objectives.Remove(id);
        }
    }

    public Objective GetObjective(string id)
    {
        objectives.TryGetValue(id, out var objective);
        return objective;
    }

    public IEnumerable<Objective> GetActiveObjectives()
    {
        return objectives.Values.Where(o => o.IsActive);
    }

    public IEnumerable<Objective> GetCompletedObjectives()
    {
        return objectives.Values.Where(o => o.IsComplete);
    }

    private void HandleObjectiveCompleted(Objective objective)
    {
        OnObjectiveCompleted?.Invoke(objective);
    }

    private void OnEnemyKilled(EnemyKilledEvent e)
    {
        var objective = GetObjective("kill_enemies");
        objective?.AddProgress(e.Count);
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

    public void Clear()
    {
        foreach (var objective in objectives.Values)
        {
            objective.OnCompleted -= HandleObjectiveCompleted;
        }
        objectives.Clear();
    }
}
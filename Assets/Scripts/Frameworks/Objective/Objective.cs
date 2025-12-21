// Core objective system
using System;
using System.Collections.Generic;
using System.Linq;


namespace Doody.Framework.ObjectiveSystem
{
    public abstract class Objective
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; } = "Name not set";
        public string Description { get; protected set; } = "Description not set.";
        public int MaxValue { get; protected set; }
        public int CurrentValue { get; protected set; } = 0;

        public bool IsComplete => CurrentValue >= MaxValue;
        public bool IsActive { get; private set; } = true;

        public event Action<Objective> OnCompleted;
        public event Action<Objective, int> OnProgressChanged;

        public virtual void AddProgress(int amount)
        {
            if (!IsActive || IsComplete) return;

            int oldValue = CurrentValue;
            CurrentValue = Math.Clamp(CurrentValue + amount, 0, MaxValue);

            if (CurrentValue != oldValue)
            {
                OnProgressChanged?.Invoke(this, CurrentValue);

                if (IsComplete)
                {
                    OnCompleted?.Invoke(this);
                    IsActive = false;
                }
            }
        }

        public virtual void SetProgress(int value)
        {
            AddProgress(value - CurrentValue);
        }

        public virtual void Reset()
        {
            CurrentValue = 0;
            IsActive = true;
        }

        // Override for custom completion logic
        public virtual bool CheckCompletion() => IsComplete;
    }

    // Different objective types
    public class CountObjective : Objective
    {
        public CountObjective(string id, string name, string description, int targetCount)
        {
            Id = id;
            Name = name;
            Description = description;
            MaxValue = targetCount;
        }
    }

    public class BooleanObjective : Objective
    {
        public BooleanObjective(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            MaxValue = 1;
        }

        public void Complete() => AddProgress(1);
    }

    public class CollectionObjective : Objective
    {
        private HashSet<string> collectedItems = new HashSet<string>();

        public CollectionObjective(string id, string name, string description, int requiredCount)
        {
            Id = id;
            Name = name;
            Description = description;
            MaxValue = requiredCount;
        }

        public void CollectItem(string itemId)
        {
            if (collectedItems.Add(itemId))
            {
                AddProgress(1);
            }
        }
    }

    // Manager to handle multiple objectives
    public class ObjectiveManager
    {
        private Dictionary<string, Objective> objectives = new Dictionary<string, Objective>();

        public event Action<Objective> OnObjectiveCompleted;
        public event Action<Objective> OnObjectiveAdded;

        public void AddObjective(Objective objective)
        {
            if (objectives.ContainsKey(objective.Id)) return;

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

        public void Clear()
        {
            foreach (var objective in objectives.Values)
            {
                objective.OnCompleted -= HandleObjectiveCompleted;
            }
            objectives.Clear();
        }
    }
}

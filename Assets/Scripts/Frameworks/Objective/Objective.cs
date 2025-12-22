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
        public CountObjective(string name, string description, int targetCount, string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? GenerateIdFromName(name) : id;
            Name = name;
            Description = description;
            MaxValue = targetCount;
        }

        private string GenerateIdFromName(string name)
        {
            return name.ToLower().Replace(" ", "_").Replace("-", "_");
        }
    }

    public class BooleanObjective : Objective
    {
        public BooleanObjective(string name, string description, string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? GenerateIdFromName(name) : id;
            Name = name;
            Description = description;
            MaxValue = 1;
        }

        private string GenerateIdFromName(string name)
        {
            return name.ToLower().Replace(" ", "_").Replace("-", "_");
        }

        public void Complete() => AddProgress(1);
    }

    public class CollectionObjective : Objective
    {
        private HashSet<string> collectedItems = new HashSet<string>();

        public CollectionObjective(string name, string description, int requiredCount, string id = null)
        {
            Id = string.IsNullOrEmpty(id) ? GenerateIdFromName(name) : id;
            Name = name;
            Description = description;
            MaxValue = requiredCount;
        }

        private string GenerateIdFromName(string name)
        {
            return name.ToLower().Replace(" ", "_").Replace("-", "_");
        }

        public void CollectItem(string itemId)
        {
            if (collectedItems.Add(itemId))
            {
                AddProgress(1);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Doody.GameEvents;

namespace Doody.Framework.ObjectiveSystem
{
    public abstract class Objective
    {
        public string Id { get; protected set; }
        public string Name { get; protected set; } = "Name not set";
        public string Description { get; protected set; } = "Description not set.";
        public int MaxValue { get; protected set; }
        public int CurrentValue { get; protected set; } = 0;

        public float ProgressPercentage => MaxValue > 0 ? (float)CurrentValue / MaxValue : 0f;

        public bool IsComplete => CurrentValue >= MaxValue;
        public bool IsActive { get; protected set; } = true;

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
                    CompleteObjective();
                }
            }
        }

        public virtual void SetProgress(int value)
        {
            AddProgress(value - CurrentValue);
        }

        public virtual void Complete()
        {
            if (!IsComplete)
            {
                CurrentValue = MaxValue;
                CompleteObjective();
            }
        }

        protected void CompleteObjective()
        {
            IsActive = false;
            OnCompleted?.Invoke(this);
        }

        public virtual void Reset()
        {
            CurrentValue = 0;
            IsActive = true;
        }

        // Override for custom completion logic
        public virtual bool CheckCompletion() => IsComplete;
    }

    // ========== PROGRESSIVE OBJECTIVES ==========
    // These track progress incrementally

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

        public bool HasItem(string itemId) => collectedItems.Contains(itemId);
    }

    // ========== COMPLETION OBJECTIVES ==========
    // These are either complete or not, with optional auto-completion

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

        public void Toggle() => Complete();
    }

    // ========== PROGRESS EVENT ==========
    // This event progresses an existing objective by ID/Name

    public class ProgressObjective
    {
        public string ObjectiveId { get; }
        public string ObjectiveName { get; }
        public int Amount { get; }

        public ProgressObjective(string objectiveIdentifier, int amount = 1)
        {
            if (objectiveIdentifier.Contains('_') || objectiveIdentifier.ToLower() == objectiveIdentifier)
            {
                ObjectiveId = objectiveIdentifier;
                ObjectiveName = null;
            }
            else
            {
                ObjectiveId = GenerateIdFromName(objectiveIdentifier);
                ObjectiveName = objectiveIdentifier;
            }

            Amount = amount;
        
        
        }

        public ProgressObjective(string objectiveId, string objectiveName, int amount = 1)
        {
            ObjectiveId = objectiveId;
            ObjectiveName = objectiveName;
            Amount = amount;
        }

        private string GenerateIdFromName(string name)
        {
            return name.ToLower().Replace(" ", "_").Replace("-", "_");
        }
    }

    // ========== COMPLETE EVENT ==========
    // This event completes an existing objective by ID/Name

    public class CompleteObjective
    {
        public string ObjectiveId { get; }
        public string ObjectiveName { get; }

        public CompleteObjective(string objectiveIdentifier)
        {
            // Check if it looks like an ID (lowercase with underscores) or a name
            if (objectiveIdentifier.Contains('_') || objectiveIdentifier.ToLower() == objectiveIdentifier)
            {
                ObjectiveId = objectiveIdentifier;
                ObjectiveName = null;
            }
            else
            {
                ObjectiveId = null;
                ObjectiveName = objectiveIdentifier;
            }
        }

        public CompleteObjective(string objectiveId, string objectiveName)
        {
            ObjectiveId = objectiveId;
            ObjectiveName = objectiveName;
        }
    }

    // ========== OBJECTIVE MANAGER ==========
    // Required to track objectives and handle Progress/Complete events

    public class ObjectiveManager
    {
        private static ObjectiveManager _instance;
        public static ObjectiveManager Instance => _instance ??= new ObjectiveManager();

        private Dictionary<string, Objective> _objectivesById = new Dictionary<string, Objective>();
        private Dictionary<string, Objective> _objectivesByName = new Dictionary<string, Objective>();

        public void Initialize()
        {
            // Subscribe to events
            Events.Subscribe<Objective>(OnObjectiveCreated);
            Events.Subscribe<ProgressObjective>(OnProgress);
            Events.Subscribe<CompleteObjective>(OnComplete);
        }

        private void OnObjectiveCreated(Objective objective)
        {
            // Register objective by ID
            if (!string.IsNullOrEmpty(objective.Id) && !_objectivesById.ContainsKey(objective.Id))
            {
                _objectivesById[objective.Id] = objective;
            }

            // Register objective by Name
            if (!string.IsNullOrEmpty(objective.Name) && !_objectivesByName.ContainsKey(objective.Name))
            {
                _objectivesByName[objective.Name] = objective;
            }

            // Subscribe to objective completion to clean up
            objective.OnCompleted += OnObjectiveCompleted;
        }

        private void OnProgress(ProgressObjective progress)
        {
            Objective objective = FindObjective(progress.ObjectiveId, progress.ObjectiveName);
            if (objective != null)
            {
                objective.AddProgress(progress.Amount);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Objective not found for progress: {progress.ObjectiveId ?? progress.ObjectiveName}");
            }
        }

        private void OnComplete(CompleteObjective complete)
        {
            Objective objective = FindObjective(complete.ObjectiveId, complete.ObjectiveName);
            if (objective != null)
            {
                objective.Complete();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Objective not found for completion: {complete.ObjectiveId ?? complete.ObjectiveName}");
            }
        }

        private Objective FindObjective(string id, string name)
        {
            // Try ID first
            if (!string.IsNullOrEmpty(id) && _objectivesById.TryGetValue(id, out var objectiveById))
            {
                return objectiveById;
            }

            // Try name if ID not found or not provided
            if (!string.IsNullOrEmpty(name) && _objectivesByName.TryGetValue(name, out var objectiveByName))
            {
                return objectiveByName;
            }

            return null;
        }

        private void OnObjectiveCompleted(Objective objective)
        {
            
        }

        public bool TryGetObjective(string identifier, out Objective objective)
        {
            objective = FindObjective(
                identifier.Contains('_') || identifier.ToLower() == identifier ? identifier : null,
                identifier.Contains('_') || identifier.ToLower() == identifier ? null : identifier
            );
            return objective != null;
        }

        public void Clear()
        {
            _objectivesById.Clear();
            _objectivesByName.Clear();
        }
    }

    // ========== HELPER EXTENSIONS ==========

    public static class ObjectiveExtensions
    {
        public static void PublishProgress(this string objectiveIdentifier, int amount)
        {
            Events.Publish(new ProgressObjective(objectiveIdentifier, amount));
        }

        public static void PublishProgress(this string objectiveIdentifier, string objectiveName, int amount)
        {
            Events.Publish(new ProgressObjective(objectiveIdentifier, objectiveName, amount));
        }

        public static void PublishComplete(this string objectiveIdentifier)
        {
            Events.Publish(new CompleteObjective(objectiveIdentifier));
        }

        public static void PublishComplete(this string objectiveIdentifier, string objectiveName)
        {
            Events.Publish(new CompleteObjective(objectiveIdentifier, objectiveName));
        }
    }

    // ========== QUICK ACCESS METHODS ==========

    public static class ObjectiveSystem
    {
        public static void Initialize() => ObjectiveManager.Instance.Initialize();

        public static void Progress(string objectiveIdentifier, int amount)
        {
            Events.Publish(new ProgressObjective(objectiveIdentifier, amount));
        }

        public static void Complete(string objectiveIdentifier)
        {
            Events.Publish(new CompleteObjective(objectiveIdentifier));
        }

        public static bool TryGetObjective(string identifier, out Objective objective)
        {
            return ObjectiveManager.Instance.TryGetObjective(identifier, out objective);
        }
    }
}
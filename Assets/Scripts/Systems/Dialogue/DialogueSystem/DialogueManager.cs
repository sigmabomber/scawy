using Doody.Framework.ObjectiveSystem;
using Doody.GameEvents;
using System.Collections.Generic;
using UnityEngine;

namespace Doody.Framework.DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance;

        [Header("Audio")]
        public AudioSource audioSource;

        // Track player's choices
        private HashSet<string> activeFlags = new HashSet<string>();
        public bool dontDestroyOnLoad = true;
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                if(dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Start a dialogue
        public void StartDialogue(DialogueTree tree)
        {
            if (tree == null)
            {
                Debug.LogError("Tree is null!");
                return;
            }

            if (tree.dialogue == null)
            {
                Debug.LogError("Tree.dialogue is null!");
                return;
            }

            if (!MeetsRequirements(tree.dialogue))
            {
                Debug.Log("Player doesn't meet requirements for this dialogue");
                return;
            }

            InputScript.InputEnabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (tree.dialogue.voiceLine != null && audioSource != null)
            {
                audioSource.PlayOneShot(tree.dialogue.voiceLine);
            }

            Events.Publish(new DialogueStartedEvent
            {
                Node = tree.dialogue,
                Tree = tree
            });
        }

        // Player selects an option
        public void ChooseOption(DialogueOption option)
        {
            if (option == null) return;

            Events.Publish(new DialogueOptionChosenEvent
            {
                Option = option,
                OptionText = option.optionText
            });

            // Apply any flags from this choice
            foreach (string flag in option.setFlags)
            {
                activeFlags.Add(flag);
                Events.Publish(new DialogueFlagSetEvent { Flag = flag });
            }

            // Execute all actions for this option
            ExecuteActions(option.actions);

            // Continue to next dialogue or end
            if (option.nextDialogue != null)
            {
                StartDialogue(option.nextDialogue);
            }
            else
            {
                EndDialogue();
            }
        }

        // Execute dialogue actions
        private void ExecuteActions(List<DialogueAction> actions)
        {
            if (actions == null || actions.Count == 0) return;

            foreach (DialogueAction action in actions)
            {
                ExecuteAction(action);
            }
        }

        // Execute a single dialogue action
        private void ExecuteAction(DialogueAction action)
        {
            if (action == null) return;

            switch (action.actionType)
            {
                case DialogueActionType.None:
                    break;

                case DialogueActionType.GiveItem:
                    if (action.item != null && InventorySystem.Instance != null)
                    {
                        InventorySystem.Instance.GiveItem(action.item, action.itemQuantity);
                    }
                    break;

                case DialogueActionType.RemoveItem:
                    if (action.item != null && InventorySystem.Instance != null)
                    {
                        InventorySystem.Instance.RemoveItem(action.item, action.itemQuantity);
                    }
                    break;

                case DialogueActionType.TeleportPlayer:
                    if (action.teleportLocation != null && PlayerController.Instance != null)
                    {
                        PlayerController.Instance.transform.position = action.teleportLocation.position;
                        PlayerController.Instance.transform.rotation = action.teleportLocation.rotation;
                    }
                    break;

                case DialogueActionType.PlaySound:
                    if (action.soundToPlay != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(action.soundToPlay);
                    }
                    break;

                case DialogueActionType.StartObjective:
                    StartObjectiveAction(action);
                    break;

                case DialogueActionType.CompleteObjective:
                    CompleteObjectiveAction(action);
                    break;

                case DialogueActionType.ProgressObjective:
                    ProgressObjectiveAction(action);
                    break;

                case DialogueActionType.SpawnObject:
                    if (action.targetObject != null && action.spawnLocation != null)
                    {
                        Instantiate(action.targetObject, action.spawnLocation.position, action.spawnLocation.rotation);
                        Debug.Log($"[Dialogue] Spawned {action.targetObject.name}");
                    }
                    break;

                case DialogueActionType.DestroyObject:
                    if (action.targetObject != null)
                    {
                        Destroy(action.targetObject);
                        Debug.Log($"[Dialogue] Destroyed {action.targetObject.name}");
                    }
                    break;

                case DialogueActionType.EnableObject:
                    if (action.targetObject != null)
                    {
                        action.targetObject.SetActive(true);
                        Debug.Log($"[Dialogue] Enabled {action.targetObject.name}");
                    }
                    break;

                case DialogueActionType.DisableObject:
                    if (action.targetObject != null)
                    {
                        action.targetObject.SetActive(false);
                        Debug.Log($"[Dialogue] Disabled {action.targetObject.name}");
                    }
                    break;

                case DialogueActionType.LoadScene:
                    if (!string.IsNullOrEmpty(action.sceneName))
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene(action.sceneName);
                        Debug.Log($"[Dialogue] Loading scene: {action.sceneName}");
                    }
                    break;

                case DialogueActionType.Custom:
                    ExecuteCustomAction(action);
                    break;
            }
        }

        private void ExecuteCustomAction(DialogueAction action)
        {
            // Method 1: Try GameObject with IDialogueCustomAction interface
            if (action.customActionTarget != null)
            {
                var customAction = action.customActionTarget.GetComponent<IDialogueCustomAction>();
                if (customAction != null)
                {
                    customAction.Execute();
                    Debug.Log($"[Dialogue] Executed custom action via interface");
                    return;
                }
            }

            // Method 2: Try event system
            if (!string.IsNullOrEmpty(action.customEventID))
            {
                DialogueEventDispatcher.Instance.TriggerEvent(action.customEventID);
                Debug.Log($"[Dialogue] Triggered event: {action.customEventID}");
                return;
            }

            // Method 3: Try UnityEvent as fallback
            if (action.customAction != null && action.customAction.GetPersistentEventCount() > 0)
            {
                action.customAction.Invoke();
                Debug.Log($"[Dialogue] Executed custom action via UnityEvent");
                return;
            }

            Debug.LogWarning("[Dialogue] Custom action configured but no valid execution method found");
        }
        private void StartObjectiveAction(DialogueAction action)
        {
            if (action.objectiveName == null || action.objectiveName.Length == 0)
            {
                Debug.LogWarning("Objective name is null or empty. Aborting");
                return;
            }

            string name = string.Join(" ", action.objectiveName);
            
            switch (action.objectiveType)
            {


                case ObjectiveTypes.Boolean:
                    string description = $"Complete: {name}";
                    Events.Publish(new BooleanObjective(name, description));
                    break;

                case ObjectiveTypes.Collective:
                case ObjectiveTypes.Count:
                    string objectiveDescription = name;

                    if (action.objectiveType == ObjectiveTypes.Count)
                    {
                        Events.Publish(new CountObjective(name, objectiveDescription, action.objectiveAmount));
                    }
                    else
                    {
                        Events.Publish(new CollectionObjective(name, objectiveDescription, action.objectiveAmount));
                    }
                    break;
            }
        }

        private void ProgressObjectiveAction(DialogueAction action)
        {
            if (action.objectiveName == null || action.objectiveName.Length == 0)
            {
                Debug.LogWarning("Objective name is null or empty. Aborting");
                return;
            }

            string name = string.Join(" ", action.objectiveName);

            switch (action.objectiveType)
            {
                case ObjectiveTypes.Boolean:
                    Events.Publish(new CompleteObjective(name));
                    break;

                case ObjectiveTypes.Collective:
                case ObjectiveTypes.Count:
                    Events.Publish(new ProgressObjective(name, action.objectiveAmount));
                    break;
            }
        }

        private void CompleteObjectiveAction(DialogueAction action)
        {
            if (action.objectiveName == null || action.objectiveName.Length == 0)
            {
                Debug.LogWarning("Objective name is null or empty. Aborting");
                return;
            }

            string name = string.Join(" ", action.objectiveName);
            Events.Publish(new CompleteObjective(name));
        }

        // End the conversation
        public void EndDialogue()
        {
            InputScript.InputEnabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Events.Publish(new DialogueEndedEvent());
        }

        // Check if player meets requirements for a dialogue node
        public bool MeetsRequirements(DialogueNode node)
        {
            if (node == null) return false;

            foreach (string flag in node.requiredFlags)
            {
                if (flag.StartsWith("!"))
                {
                    string flagName = flag.Substring(1);
                    if (activeFlags.Contains(flagName))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!activeFlags.Contains(flag))
                    {
                        Debug.Log($"[Dialogue] Missing required flag: {flag}");
                        return false;
                    }
                }
            }
            return true;
        }

        // Check if player meets requirements for a dialogue option
        public bool MeetsOptionRequirements(DialogueOption option)
        {
            if (option == null) return false;

            foreach (string flag in option.requiredFlags)
            {
                if (flag.StartsWith("!"))
                {
                    string flagName = flag.Substring(1);
                    if (activeFlags.Contains(flagName))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!activeFlags.Contains(flag))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Utility methods
        public void SetFlag(string flag)
        {
            activeFlags.Add(flag);
            Events.Publish(new DialogueFlagSetEvent { Flag = flag });
        }

        public void ClearFlag(string flag) => activeFlags.Remove(flag);
        public bool HasFlag(string flag) => activeFlags.Contains(flag);
        public void ClearAllFlags() => activeFlags.Clear();

        public string[] GetAllFlags()
        {
            string[] flagArray = new string[activeFlags.Count];
            activeFlags.CopyTo(flagArray);
            return flagArray;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }

    // Interface for custom actions that can be attached to GameObjects
    public interface IDialogueCustomAction
    {
        void Execute();
    }
}
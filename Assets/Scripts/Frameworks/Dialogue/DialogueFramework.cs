using Doody.GameEvents;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

// DIALOGUE EVENTS
namespace Doody.Framework.DialogueSystem
{
    public struct DialogueStartedEvent
    {
        public DialogueNode Node;
        public DialogueTree Tree;
    }

    public struct DialogueEndedEvent { }

    public struct DialogueOptionChosenEvent
    {
        public DialogueOption Option;
        public string OptionText;
    }

    public struct DialogueFlagSetEvent
    {
        public string Flag;
    }

    public struct DialogueFailedEvent
    {
        public DialogueTree Tree;
        public string Reason;
    }
}

// DIALOGUE NODE - Each piece of dialogue
[System.Serializable]

public class DialogueNode
{
    [Header("Dialogue Content")]
    [TextArea(3, 6)]
    public string dialogueText;

    [Tooltip("Drag your audio clip here")]
    public AudioClip voiceLine;

    [Header("Typewriter Settings")]
    [Tooltip("Characters per second. 0 = use default speed from DialogueUI")]
    public float typewriterSpeed = 0f;

    [Header("Next Dialogue (No Options)")]
    [Tooltip("If there are no player options, this dialogue will play next. Leave empty to end conversation.")]
    public DialogueTree nextDialogue;

    [Header("Player Choices")]
    public List<DialogueOption> options = new List<DialogueOption>();

    [Header("Conditions (Optional)")]
    [Tooltip("Leave empty if this node is always available")]
    public List<string> requiredFlags = new List<string>();
}
public enum DialogueActionType
{
    None,
    GiveItem,
    RemoveItem,
    
    TeleportPlayer,
    PlaySound,

    StartObjective,
    ProgressObjective,
    CompleteObjective,
    SpawnObject,
    DestroyObject,
    EnableObject,
    DisableObject,
    LoadScene,
    Custom 
}

public enum ObjectiveTypes
{

    Boolean,
    Collective,
    Count
}

[System.Serializable]
public class DialogueAction
{
    [Header("Action Type")]
    public DialogueActionType actionType = DialogueActionType.None;

    [Header("Item Actions (GiveItem/RemoveItem)")]
    public ItemData item;
    public int itemQuantity = 1;


    [Header("Teleport Action")]
    public Transform teleportLocation;

    [Header("Sound Action")]
    public AudioClip soundToPlay;

    [Header("Objective Actions (StartObjective/CompleteObjective)")]
    public string objectiveName;
    public ObjectiveTypes objectiveType;

    [Header ("If objective is boolean ignore")]
    public int objectiveAmount;

    [Header("Object Actions (Spawn/Destroy/Enable/Disable)")]
    public GameObject targetObject;
    public Transform spawnLocation;

    [Header("Scene Action (LoadScene)")]
    public string sceneName;

    [Header("Custom Action")]
    public UnityEvent customAction;
}

[System.Serializable]
public class DialogueOption
{
    [TextArea(2, 3)]
    public string optionText;

    [Tooltip("Which dialogue comes next? Leave empty to end conversation")]
    public DialogueTree nextDialogue;

    [Header("Requirements (Optional)")]
    [Tooltip("Flags needed to show this option (supports !Flag for negative checks)")]
    public List<string> requiredFlags = new List<string>();

    [Header("Effects (Optional)")]
    [Tooltip("Flags to set when this option is chosen")]
    public List<string> setFlags = new List<string>();

    [Header("Actions (Optional)")] 
    [Tooltip("Actions to perform when this option is chosen")]
    public List<DialogueAction> actions = new List<DialogueAction>(); 
}






#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(DialogueAction))]
public class DialogueActionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float width = position.width;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // Draw default label (Element 0)
        Rect rect = new Rect(position.x, y, width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(rect, label);
        y += rect.height + spacing;

        EditorGUI.indentLevel++;

        SerializedProperty actionTypeProp = property.FindPropertyRelative("actionType");

        // Action Type dropdown
        rect.height = EditorGUI.GetPropertyHeight(actionTypeProp);
        EditorGUI.PropertyField(new Rect(position.x, y, width, rect.height), actionTypeProp);
        y += rect.height + spacing;

        DialogueActionType actionType =
            (DialogueActionType)actionTypeProp.enumValueIndex;

        // Draw only required fields
        DrawByActionType(property, actionType, position.x, ref y, width);

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private void DrawByActionType(
        SerializedProperty property,
        DialogueActionType actionType,
        float x,
        ref float y,
        float width)
    {
        switch (actionType)
        {
            case DialogueActionType.GiveItem:
            case DialogueActionType.RemoveItem:
                DrawField(property, "item", x, ref y, width);
                DrawField(property, "itemQuantity", x, ref y, width);
                break;

           

            case DialogueActionType.TeleportPlayer:
                DrawField(property, "teleportLocation", x, ref y, width);
                break;

            case DialogueActionType.PlaySound:
                DrawField(property, "soundToPlay", x, ref y, width);
                break;

            case DialogueActionType.StartObjective:
                DrawField(property, "objectiveName", x, ref y, width);
                DrawField(property, "objectiveType", x, ref y, width);

                break;
            case DialogueActionType.CompleteObjective:

                DrawField(property, "objectiveName", x, ref y, width);
                break;
            case DialogueActionType.ProgressObjective:
                DrawField(property, "objectiveName", x, ref y, width);
               
                DrawField(property, "objectiveAmount", x, ref y, width);
                break;

            case DialogueActionType.SpawnObject:
                DrawField(property, "targetObject", x, ref y, width);
                DrawField(property, "spawnLocation", x, ref y, width);
                break;

            case DialogueActionType.DestroyObject:
            case DialogueActionType.EnableObject:
            case DialogueActionType.DisableObject:
                DrawField(property, "targetObject", x, ref y, width);
                break;

            case DialogueActionType.LoadScene:
                DrawField(property, "sceneName", x, ref y, width);
                break;

            case DialogueActionType.Custom:
                DrawField(property, "customAction", x, ref y, width, true);
                break;
        }
    }

    private void DrawField(
        SerializedProperty root,
        string name,
        float x,
        ref float y,
        float width,
        bool includeChildren = false)
    {
        SerializedProperty prop = root.FindPropertyRelative(name);
        float h = EditorGUI.GetPropertyHeight(prop, includeChildren);

        EditorGUI.PropertyField(
            new Rect(x, y, width, h),
            prop,
            includeChildren
        );

        y += h + EditorGUIUtility.standardVerticalSpacing;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // label
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty actionTypeProp = property.FindPropertyRelative("actionType");
        height += EditorGUI.GetPropertyHeight(actionTypeProp) + spacing;

        DialogueActionType actionType =
            (DialogueActionType)actionTypeProp.enumValueIndex;

        height += GetActionHeight(property, actionType);

        return height;
    }

    private float GetActionHeight(SerializedProperty property, DialogueActionType actionType)
    {
        float h = 0f;
        float space = EditorGUIUtility.standardVerticalSpacing;

        void Add(string name, bool children = false)
        {
            h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative(name), children) + space;
        }

        switch (actionType)
        {
            case DialogueActionType.GiveItem:
            case DialogueActionType.RemoveItem:
                Add("item");
                Add("itemQuantity");
                break;

         

            case DialogueActionType.TeleportPlayer:
                Add("teleportLocation");
                break;

            case DialogueActionType.PlaySound:
                Add("soundToPlay");
                break;

            case DialogueActionType.StartObjective:
                Add("objectiveName");
                Add("objectiveType");

                break;
            case DialogueActionType.CompleteObjective:
                Add("objectiveName");
                
                break;

            case DialogueActionType.ProgressObjective:

                Add("objectiveName");
           
                Add("objectiveAmount");
                break;

            case DialogueActionType.SpawnObject:
                Add("targetObject");
                Add("spawnLocation");
                break;

            case DialogueActionType.DestroyObject:
            case DialogueActionType.EnableObject:
            case DialogueActionType.DisableObject:
                Add("targetObject");
                break;

            case DialogueActionType.LoadScene:
                Add("sceneName");
                break;

            case DialogueActionType.Custom:
                Add("customAction", true);
                break;
        }

        return h;
    }
}
#endif

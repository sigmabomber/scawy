using UnityEngine;
using System.Collections.Generic;

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

    [Header("Player Choices")]
    public List<DialogueOption> options = new List<DialogueOption>();

    [Header("Conditions (Optional)")]
    [Tooltip("Leave empty if this node is always available")]
    public List<string> requiredFlags = new List<string>();
}

// DIALOGUE OPTION - Player choices
[System.Serializable]
public class DialogueOption
{
    [TextArea(2, 3)]
    public string optionText;

    [Tooltip("Which dialogue comes next? Leave empty to end conversation")]
    public DialogueTree nextDialogue;

    [Header("Effects (Optional)")]
    [Tooltip("Flags to set when this option is chosen")]
    public List<string> setFlags = new List<string>();
}


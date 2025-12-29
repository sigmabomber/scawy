using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Tree")]
public class DialogueTree : ScriptableObject
{
    [Header("Dialogue Setup")]
    public DialogueNode dialogue;

    [Header("Speaker Info (Optional)")]
    public string speakerName;
    public Sprite speakerPortrait;
}


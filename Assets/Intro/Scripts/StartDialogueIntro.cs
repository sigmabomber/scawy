using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;
using Doody.Framework.DialogueSystem;

public class StartDialogueIntro : EventListener
{
    public DialogueTree dialogue;
   



    public void StartDialogue()
    {

        DialogueManager.Instance.StartDialogue(dialogue);
    }




}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Doody.GameEvents;
public class LeverSystem : MonoBehaviour, IInteractable
{
    private bool canInteract = true;

    public string leverId;
    public static bool powerOn = true;
    public AnimationClip durationClip;
    public float duration;
    private Animator animator;
    protected int PullHash;
    protected int DurationHash;

    public float timeBeforeTurningOn;
    private void Start()
    {
        animator = GetComponent<Animator>();
        PullHash = Animator.StringToHash("Pull");
        DurationHash = Animator.StringToHash("Duration");
    }
    public void Interact() 
    {

        if (!powerOn)
        {
            InteractionSystem.Instance.ShowFeedback("No Power!", Color.red);
        }

        canInteract = false;
       
        animator.SetTrigger(PullHash);

        StartCoroutine(StartLever());

    }


    private IEnumerator StartLever()
    {
        yield return new WaitForSeconds(timeBeforeTurningOn);
        animator.SetTrigger(DurationHash);

        animator.speed = durationClip.length / duration;
        Events.Publish(new TurnOnLights(leverId));

    }
    public void ResetLever()
    {
        Events.Publish(new TurnOffLights(leverId));
        canInteract = true;
    }
    public bool CanInteract() { return canInteract; }
    public string GetInteractionPrompt() 
    {
        return "Turn on Lights";
    }
    public Sprite GetInteractionIcon() { return null; }
}

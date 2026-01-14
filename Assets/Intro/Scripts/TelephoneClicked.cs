using Doody.GameEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
public class TelephoneClicked : MonoBehaviour
{
    private bool cutsceneActive = false;
    public PlayableDirector director;
    public TimelineAsset timeline2;
    public CameraMouseRotate cam;
    public MouseHoverScript ms;
    void Start()
    {
        Events.Subscribe<ItemClicked>(PickUpPhone, this);
    }

  public void PickUpPhone(ItemClicked data)
    {
        if (cutsceneActive || data.GameObject != gameObject) return;
        cutsceneActive = true;
        cam.enabled = false;
        ms.enabled = false;
        transform.GetComponent<AudioSource>().enabled = false;
        director.playableAsset = timeline2;

        director.Play();
    }
}

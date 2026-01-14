using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
public class Tween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private Transform text;

    private Vector2 originalSize;
    public Vector2 hoverSize;
    public float duration;


    private void Start()
    {
        text = GetComponent<Transform>();
        originalSize = transform.localScale;
    }
    public void OnPointerEnter(PointerEventData data)
    {
        text.DOScale(new Vector2(originalSize.x + hoverSize.x, originalSize.y + hoverSize.y),duration);
    }
    public void OnPointerExit(PointerEventData data )
    {
        text.DOScale(new Vector2(originalSize.x,originalSize.y),duration);
    }
}

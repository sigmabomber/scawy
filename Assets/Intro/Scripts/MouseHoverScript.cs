using UnityEngine;
using System.Collections.Generic;
using Doody.GameEvents;

public class MouseHoverScript : MonoBehaviour
{
    public LayerMask layer;
    private Dictionary<GameObject, Outline> outlineCache = new();
    private GameObject currentHoveredObject;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layer))
        {
            GameObject hitObject = hit.transform.gameObject;

            // Check for click
            if (Input.GetMouseButtonDown(0))
            {
                Events.Publish(new ItemClicked(currentHoveredObject));
            }

            // If we're hovering over a new object
            if (hitObject != currentHoveredObject)
            {
                // Disable outline on previously hovered object
                if (currentHoveredObject != null && outlineCache.ContainsKey(currentHoveredObject))
                {
                    outlineCache[currentHoveredObject].enabled = false;
                }

                // Get or add outline to cache
                if (!outlineCache.ContainsKey(hitObject))
                {
                    Outline outline = hitObject.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outlineCache[hitObject] = outline;
                    }
                }

                // Enable outline on current object
                if (outlineCache.ContainsKey(hitObject))
                {
                    outlineCache[hitObject].enabled = true;
                }

                currentHoveredObject = hitObject;
            }
        }
        else
        {
            // Not hovering over anything, disable current outline
            if (currentHoveredObject != null && outlineCache.ContainsKey(currentHoveredObject))
            {
                outlineCache[currentHoveredObject].enabled = false;
                currentHoveredObject = null;
            }
        }
    }
    private void OnDisable()
    {
        foreach (Outline outline in outlineCache.Values)
        {
            outline.enabled = false;
        }
    }
}
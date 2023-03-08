using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoveringText : MonoBehaviour
{
    public Text hoveringText;
    public InventoryManager inv;
    public Vector3 offset;

    public int invLayer;

    public void Start()
    {
        invLayer = LayerMask.NameToLayer("inventory");
    }

    // Update is called once per frame
    void Update()
    {
        hoveringText.transform.position = Input.mousePosition + offset;
        if (inv.FindClosestSlot() != null && inv.FindClosestSlot().item != null)
        {
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = inv.FindClosestSlot().item.name;
        }
        else if (inv.isMovingItem && !IsPointerOverInvElement())
        {
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "Drop";
        }
        else if(!IsPointerOverInvElement())
        {
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "Walk";
        }
        else
        {
            hoveringText.gameObject.SetActive(false);
        }
    }
    
    public bool IsPointerOverInvElement()
    {
        return IsPointerOverinvElementRayCast(GetEventSystemRaycastResults());
    }

    
    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverinvElementRayCast(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == invLayer)
                return true;
        }
        return false;
    }

    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
}

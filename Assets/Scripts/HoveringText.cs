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
    public Camera mainCamera;
    public GameObject marker;

    public int invLayer;

    public void Start()
    {
        invLayer = LayerMask.NameToLayer("inventory");
        
        marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
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
            if(IsPointerOverComponent<HealthComponent>())
                hoveringText.text = "Attack";
            else
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
    
    public bool IsPointerOverComponent<T>() where T : Component
    {
        return IsPointerOverComponentRaycast<T>();
    }
    
    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverinvElementRayCast(List<RaycastResult> eventSystemRaycastResults)
    {
        for (int index = 0; index < eventSystemRaycastResults.Count; index++)
        {
            RaycastResult curRaycastResult = eventSystemRaycastResults[index];
            if (curRaycastResult.gameObject.layer == invLayer)
                return true;
        }
        return false;
    }
    
    private bool IsPointerOverComponentRaycast<T>() where T : Component
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z; // This value should be adjusted depending on the positions of your game objects

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.collider.gameObject.GetComponent<T>())
            {
                return true;
            }
        }

        return false;
    }

    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }
}

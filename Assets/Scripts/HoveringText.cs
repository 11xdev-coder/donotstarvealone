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
    public PlayerController player;

    public RaycastHit2D hit;

    // Update is called once per frame
    void Update()
    {
        hoveringText.transform.position = Input.mousePosition + offset;
        // put checks without IsPoinerOverInvElement first
        if (inv.FindClosestSlotItem() != null && inv.FindClosestSlotItem().item != null)
        {
            player.canMoveToMouse = false;
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = inv.FindClosestSlotItem().item.name;
        }
        else if (inv.IsOverSlot())
        {
            player.canMoveToMouse = false;
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "";
        }
        else if (inv.isMovingItem && !inv.IsOverSlot())
        {
            hoveringText.gameObject.SetActive(true);
            hoveringText.text = "Drop";
        }
        else if(!inv.IsOverSlot())
        {
            hoveringText.gameObject.SetActive(true);
            if (IsPointerOverComponent<HealthComponent>())
            {
                hoveringText.text = "Attack";
                if(Input.GetMouseButtonDown(0))
                    player.SetAttackTarget(hit.collider.gameObject);
            }
            else
            {
                player.canMoveToMouse = true;
                hoveringText.text = "Walk";
            }
                
        }
        else
        {
            hoveringText.gameObject.SetActive(false);
        }
    }
    
    private bool IsPointerOverComponent<T>() where T : Component
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z; // This value should be adjusted depending on the positions of your game objects

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);
        
        hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider != null)
        {
            if (hit.collider.gameObject.GetComponent<T>())
            {
                return true;
            }
        }

        return false;
    }
}

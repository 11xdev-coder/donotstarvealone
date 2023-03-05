using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoveringText : MonoBehaviour
{
    public Text hoveringText;
    public InventoryManager inv;
    public Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        hoveringText.transform.position = Input.mousePosition + offset;
        if (inv.FindClosestSlot() != null && inv.FindClosestSlot().item != null)
        {
            hoveringText.text = inv.FindClosestSlot().item.name;
        }
        else if(!IsCursorOverHUD())
        {
            hoveringText.text = "Walk";
        }
        else
        {
            hoveringText.text = "";
        }
        print(IsCursorOverHUD());
    }

    public bool IsCursorOverHUD()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}

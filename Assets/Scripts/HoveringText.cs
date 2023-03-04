using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HoveringText : MonoBehaviour
{
    public Text hoveringText;
    public InventoryManager inv;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        hoveringText.transform.localPosition = Input.mousePosition;
        if(inv.FindClosestSlot() != null)
            hoveringText.text = inv.FindClosestSlot().item.name;
    }
}

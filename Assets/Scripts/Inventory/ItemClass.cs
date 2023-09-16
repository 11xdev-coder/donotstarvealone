using System.Collections.Generic;
using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item")]
    public string itemName;
    public Sprite itemSprite;
    public GameObject droppedItemPrefab;
    public bool isStackable;
    public int maxStack = 20;
    public int damage;
    public Vector3 handScale;

    [Header("Mining")] 
    public int pickaxePower;
    public int axePower;
    
    [Header("Display Info")]
    public List<DisplayTextEntry> displayTextEntries = new List<DisplayTextEntry>();

    protected virtual void Use(PlayerController caller)
    {
        
    }
    
    #region Drop
    
    public void DropItem(SlotClass slot, Transform dropTransform, InventoryManager inventoryManager)
    {
        // Create the dropped item object using the prefab
        var position = dropTransform.position;
        GameObject droppedItem = Instantiate(droppedItemPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);

        // Assign the item's data to the dropped item
        droppedItem.GetComponent<DroppedItem>().Initialize(slot.item, slot.count);

        // Remove the item from the inventory
        slot.Clear();
        inventoryManager.isMovingItem = false;
    }
    
    public void SpawnItemAsDropped(SlotClass slot, Transform dropTransform)
    {
        // Create the dropped item object using the prefab
        var position = dropTransform.position;
        GameObject droppedItem = Instantiate(droppedItemPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);

        // Assign the item's data to the dropped item
        droppedItem.GetComponent<DroppedItem>().Initialize(slot.item, slot.count);
    }
    
    #endregion
    
    #region Display
    
    [System.Serializable]
    public class DisplayTextEntry
    {
        public bool enabled = true; // This allows you to toggle display on/off for each entry directly in the editor.
        public string label;       // Label like "Damage", "Name", etc.
        public DisplayType displayType;

        public enum DisplayType
        {
            Description, 
            DamageValue    // The damage value of the item
        }
    }
    
    public List<string> GetDisplayInfo()
    {
        List<string> info = new List<string>();
        info.Add(itemName); // name by default
        
        if(GetTool() != null) info.Add("RMB: Equip/Unequip");
        
        foreach(var entry in displayTextEntries)
        {
            if (!entry.enabled) continue;

            switch(entry.displayType)
            {
                case DisplayTextEntry.DisplayType.Description:
                    info.Add(entry.label);
                    break;
                case DisplayTextEntry.DisplayType.DamageValue:
                    if(damage > 0)
                        info.Add(entry.label + ": " + damage.ToString());
                    break;
            }
        }

        return info;
    }
    
    #endregion
    
    public virtual ItemClass GetItem()
    {
        return this;
        
    }

    public virtual ToolClass GetTool()
    {
        return null; 
    }

    public virtual MiscClass GetMisc()
    {
        return null; 
    }

    public virtual ConsumableClass GetConsumable()
    {
        return null;
    }
}

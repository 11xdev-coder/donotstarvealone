using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class InventoryManager : MonoBehaviour
{
    public GameObject slotHolder;
    
    public SlotClass[] startingItems;
    public SlotClass[] items;
    //public List<SlotClass> equips = new List<SlotClass>();

    public GameObject[] slots;

    
    public delegate void ToolChangedAction(ItemClass newTool);
    public event ToolChangedAction OnToolChanged;
    
    [Header("Tool")]
    public int equippedToolIndex;
    public bool isEquippedTool;
    public SlotClass equippedTool;
    public ItemClass previouslyEquippedTool;

    [Header("Moving")] 
    public GameObject itemCursor;
    public SlotClass originalSlot;
    public SlotClass tempSlot;
    public SlotClass movingSlot;
    public bool isMovingItem;
    
    public void Start()
    {
        // set slots to amount of children that have "Inventory" panel
        slots = new GameObject[slotHolder.transform.childCount];
        items = new SlotClass[slots.Length];
            
        for (int s = 0; s < items.Length; s++)
            items[s] = new SlotClass();

        // set all the slots
        for (int i = 0; i < slotHolder.transform.childCount; i++)
            slots[i] = slotHolder.transform.GetChild(i).gameObject;
        
        // add all starting items
        foreach (var startingItem in startingItems)
            Add(startingItem.item, startingItem.count);
        
        equippedToolIndex = items.Length - 1;
        Refresh();
    }

    public void Update()
    {
        // setting active item cursor if me moving item
        itemCursor.SetActive(isMovingItem);
        itemCursor.transform.position = Input.mousePosition;
        if (isMovingItem)
        {
            itemCursor.GetComponentInChildren<Image>().sprite = movingSlot.item.itemSprite;
            itemCursor.GetComponentInChildren<Text>().text = movingSlot.item.isStackable ? movingSlot.count.ToString() : "";
        }
            
        
        if (Input.GetMouseButtonDown(0)) // left click
        {
            if (Input.GetKey(KeyCode.LeftControl)) // if we hold control
            {
                if (isMovingItem)
                    PutSingle();
                else
                    TakeHalf();
            }
            else
                ItemMoveOnLeftClick();
        }
        // if there is an item in tool slot
        if (items[equippedToolIndex].item != null)
        {
            // check if it isnt a tool and we are hovered on tool slot where this item is
            if (items[equippedToolIndex].item.GetTool() == null && FindClosestSlotItem() != null && items[equippedToolIndex].item != null &&
                FindClosestSlotItem().item == items[equippedToolIndex].item)
            {
                // start moving item so it wouldnt be put in to a slot
                ItemMoveOnLeftClick();
            }
        }
        
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            HandleItemRightClick();
        }

        // TOOL SWAP CHECKS
        
        // If the currently equipped tool is different from the previously equipped one
        if (items[equippedToolIndex].item != previouslyEquippedTool)
        {
            // Update the previously equipped tool
            previouslyEquippedTool = items[equippedToolIndex].item;

            // Check if there is a tool equipped and set the flag accordingly
            isEquippedTool = items[equippedToolIndex].item != null;

            // Trigger the OnToolChanged event
            OnToolChanged?.Invoke(isEquippedTool ? items[equippedToolIndex].item : null);
        }
        
        if (items[equippedToolIndex].item != null)
            equippedTool = items[equippedToolIndex];
    }

    public bool IsInventoryFull()
    {
        foreach (var item in items)
        {
            if (item.item == null) return false;
        }

        return true;
    }

    private void ItemMoveOnLeftClick()
    {
        if (isMovingItem) // end item move
            EndItemMove();
        else
            BeginItemMove();
    }

    #region Item Stuff

    private void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            try
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = items[i].item.itemSprite;
                slots[i].transform.GetChild(1).GetComponent<Text>().text = items[i].item.isStackable ? items[i].count.ToString() : "";
            }
            catch
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = null;
                slots[i].transform.GetChild(1).GetComponent<Text>().text = "";
            }

            if (items[i].item != null)
            {
                if(items[i].count == 0) items[i].Clear();
            }
        }
    }
    
    public int Add(ItemClass item, int count)
    {
        
        SlotClass slot = Contains(item);

        // Step 1: Try to add to existing slot
        if (slot != null && slot.item.isStackable && slot.count < item.maxStack)
        {
            int countCanAdd = item.maxStack - slot.count; 
            int countToAdd = Mathf.Min(count, countCanAdd); 

            slot.AddCount(countToAdd);
            count -= countToAdd;
        }

        // Step 2: If there are more items, try to find another slot with the same item, but not full
        for (int i = 0; i < items.Length && count > 0; i++)
        {
            if (items[i].item == item && items[i].count < item.maxStack)
            {
                int countCanAdd = item.maxStack - items[i].count;
                int countToAdd = Mathf.Min(count, countCanAdd);

                items[i].AddCount(countToAdd);
                count -= countToAdd;
            }
        }

        // Step 3: If there are any remaining items to add, find an empty slot
        for (int i = 0; i < items.Length && count > 0; i++)
        {
            if (i != equippedToolIndex && items[i].item == null) 
            {
                int quantityCanAdd = item.maxStack; 
                int quantityToAdd = Mathf.Min(count, quantityCanAdd);

                items[i].AddItem(item, quantityToAdd);

                count -= quantityToAdd;
            }
        }
        
        if(count > 0 && movingSlot.count < item.maxStack)
        {
            int countCanAddToMovingSlot = Mathf.Min(item.maxStack - movingSlot.count, count);
            movingSlot.AddItem(item, countCanAddToMovingSlot);
            count -= countCanAddToMovingSlot;
            isMovingItem = true;
        }
        
        Refresh();

        return count; // Returns true if all items were added, false otherwise
    }


    public void UseSelected(ItemClass item)
    {
        Remove(item, 1);
        Refresh();
    }

    public bool Remove(ItemClass item, int count)
    {
        // if the item is in movingSlot
        if (isMovingItem && movingSlot.item == item)
        {
            // remove it
            if (movingSlot.count >= count)
            {
                // -1
                movingSlot.SubCount(count);
                if (movingSlot.count <= 0) // if no left
                {
                    movingSlot.Clear(); // clear it
                    isMovingItem = false;
                }
                Refresh();
                return true;
            }
            else
            {
                // if movingSlot doesnt have enough items, subtract what we can and continue to main inventory
                count -= movingSlot.count;
                movingSlot.Clear();
                isMovingItem = false;
            }
        }

        // continue to remove from main inventory
        SlotClass slot = Contains(item);
        if (slot != null)
        {
            if (slot.count >= count)
            {
                slot.SubCount(count);
                if (slot.count <= 0 && slot != movingSlot)
                {
                    slot.Clear();
                }
            }
            else
            {
                // if not enough items in the slot, return false indicating partial or no removal
                Refresh();
                return false;
            }
        }
        else
        {
            // if item not found in inventory, return false
            Refresh();
            return false;
        }

        Refresh();
        return true;
    }


    public SlotClass Contains(ItemClass item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == item)
                return items[i];
        }

        return null;
    }
    
    public bool ContainsBool(ItemClass item, int count)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == item && items[i].count >= count)
                return true;
        }

        return false;
    }
    
    #endregion

    #region Moving Stuff

    private void HandleItemRightClick()
    {
        SlotClass clickedSlot = FindClosestSlotItem();
        if (clickedSlot == null || clickedSlot.item == null) 
            return; // No item to interact with.

        int clickedIndex = Array.IndexOf(items, clickedSlot);

        // If we right-clicked the equipped tool.
        if(clickedIndex == equippedToolIndex)
        {
            UnequipTool();
            return;
        }
    
        ToolClass clickedTool = clickedSlot.item.GetTool();
        if (clickedTool != null) // We right-clicked on a tool in the inventory.
        {
            // If there's already a tool equipped.
            if (items[equippedToolIndex].item != null)
            {
                // Swap tools.
                SlotClass temp = new SlotClass(items[equippedToolIndex]);
                items[equippedToolIndex].AddItem(clickedSlot.item, clickedSlot.count);
                clickedSlot.AddItem(temp.item, temp.count);
            }
            else
            {
                // Equip the tool.
                items[equippedToolIndex].AddItem(clickedSlot.item, clickedSlot.count);
                clickedSlot.Clear();
            }
            Refresh();
        }
    }
    
    private void UnequipTool()
    {
        // If the inventory is full, do nothing for now.
        if (IsInventoryFull()) 
            return;

        // Find the first available slot.
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == null)
            {
                items[i].AddItem(items[equippedToolIndex].item, items[equippedToolIndex].count);
                items[equippedToolIndex].Clear();
                break;
            }
        }
        Refresh();
    }
    
    public bool BeginItemMove()
    {
        originalSlot = FindClosestSlotItem();
        if (originalSlot == null || originalSlot.item == null)
            return false; // no item to move

        movingSlot = new SlotClass(originalSlot);
        originalSlot.Clear(); // clearing slot we clicked on since we picked up the item
        isMovingItem = true;
        Refresh();
        return true;
    }
    
    public bool TakeHalf()
    {
        originalSlot = FindClosestSlotItem();
        if (originalSlot == null || originalSlot.item == null)
            return false; // no item to take half from

        movingSlot = new SlotClass(originalSlot.item, Mathf.CeilToInt(originalSlot.count / 2f)); // setting the moving slot to item we clicked on but with half count
        originalSlot.SubCount(Mathf.CeilToInt(originalSlot.count / 2f)); // taking half from orig slot
        if(originalSlot.count < 1)
            originalSlot.Clear();
        
        isMovingItem = true;
        Refresh();
        return true;
    }

    public bool EndItemMove()
    {
        originalSlot = FindClosestSlotItem();
        if (originalSlot == null)
        {
            // for now do nothing
            return false;
        }
        else // clicked on slot
        {
            if (originalSlot.item != null) // slot isnt empty
            {
                if (originalSlot.item == movingSlot.item && 
                    originalSlot.item.isStackable && originalSlot.count < originalSlot.item.maxStack) // items should stack
                {
                    var countCanAdd = originalSlot.item.maxStack - originalSlot.count;
                    var countToAdd = Mathf.Clamp(movingSlot.count, 0, countCanAdd);
                    var remainder = movingSlot.count - countToAdd;
                    
                    originalSlot.AddCount(countToAdd);
                    if (remainder <= 0) movingSlot.Clear();
                    else
                    {
                        movingSlot.SubCount(countCanAdd);
                        Refresh();
                        return false;
                    }
                        
                }
                else
                {
                    tempSlot = new SlotClass(originalSlot);
                    originalSlot.AddItem(movingSlot.item, movingSlot.count);
                    movingSlot.AddItem(tempSlot.item, tempSlot.count);
                    Refresh();
                    return true;
                }
            }
            else // place item as usual
            {
                originalSlot.AddItem(movingSlot.item, movingSlot.count);
                movingSlot.Clear();
            }
        }

        isMovingItem = false;
        Refresh();
        return true;
    }
    
    public bool PutSingle()
    {
        originalSlot = FindClosestSlotItem();
        
        if (originalSlot == null)
            return false;

        if (originalSlot.item != null && 
            (originalSlot.item != movingSlot.item || originalSlot.count >= originalSlot.item.maxStack))
            return false;
        
        movingSlot.SubCount(1);
        if (originalSlot.item != null && originalSlot.item == movingSlot.item)
            originalSlot.AddCount(1);
        else
            originalSlot.AddItem(movingSlot.item, 1);    
        
        if (movingSlot.count < 1)
        {
            isMovingItem = false;
            movingSlot.Clear();
        }
        else
            isMovingItem = true;
        
        Refresh();
        return true;
    }
    
    public SlotClass FindClosestSlotItem()
    {
        foreach (GameObject slotGameObject in slots)
        {
            PointerCheck slot = slotGameObject.GetComponent<PointerCheck>();
            if (slot != null && slot.IsMouseOver())
            {
                int index = Array.IndexOf(slots, slotGameObject);
                return items[index];
            }
        }

        return null;
    }
    
    public GameObject FindClosestSlotObject()
    {
        foreach (GameObject slotGameObject in slots)
        {
            PointerCheck slot = slotGameObject.GetComponent<PointerCheck>();
            if (slot != null && slot.IsMouseOver())
            {
                return slotGameObject;
            }
        }

        return null;
    }

    public bool IsOverSlot()
    {
        foreach (GameObject slotGameObject in slots)
        {
            PointerCheck slot = slotGameObject.GetComponent<PointerCheck>();
            if (slot != null && slot.IsMouseOver())
            {
                return true;
            }
        }

        return false;
    }
    
    #endregion
}

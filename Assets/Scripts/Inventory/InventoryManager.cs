using System;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class InventoryManager : MonoBehaviour
{
    public Object[] crafts;
    
    
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

    public TalkerComponent talker;

    public void Start()
    {
        crafts = Resources.LoadAll("Recipes", typeof(CraftingRecipeClass));
        
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

    private void Craft(CraftingRecipeClass craft)
    {
        if (craft.CanCraft(this))
        {
            craft.Craft(this);
        }
        else
        {
            if (talker != null) talker.Say("Cant craft!");
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Craft((CraftingRecipeClass)crafts[0]);
        }
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
            // check if it isnt a tool
            if (items[equippedToolIndex].item.GetTool() == null)
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
    
    public bool Add(ItemClass item, int count)
    {
        //check if inventory contains item
        SlotClass slot = Contains(item);

        if (slot != null && slot.item.isStackable && slot.count < item.maxStack)
        {
            // going to add 20 = count
            // there is already 5 = slot.count;
            var countCanAdd = slot.item.maxStack - slot.count; //16 - 5 = 11
            var countToAdd = Mathf.Clamp(count, 0, countCanAdd);
                
            var remainder = count - countCanAdd; // = 9
            
            slot.AddCount(countToAdd);
            if (remainder > 0) Add(item, remainder);
        }
        else
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].item == null) //this is an empty slot
                { 
                    var quantityCanAdd = item.maxStack - items[i].count; //16 - 5 = 11
                    var quantityToAdd = Mathf.Clamp(count, 0, quantityCanAdd);
                
                    var remainder = count - quantityCanAdd; // = 9
            
                    items[i].AddItem(item, quantityToAdd);
                    if (remainder > 0) Add(item, remainder);
                    break;
                }
            }
        }

        Refresh();
        return true;
    } 

    public void UseSelected(ItemClass item)
    {
        Remove(item, 1);
        Refresh();
    }

    public bool Remove(ItemClass item, int count)
    {
        SlotClass temp = Contains(item);
        if (temp != null)
        {
            if(temp.count > 1)
                temp.SubCount(count);
            else
            {
                int removeSlotIndex = 0;
                for(int index = 0; index < items.Length; index++)
                {
                    if (items[index].item == item)
                    {
                        removeSlotIndex = index;
                        break;
                    }
                }

                items[removeSlotIndex].Clear();
            }
        }
        else
            return false;
        
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
        for (int s = 0; s < slots.Length; s++)
        {
            if (Vector2.Distance(slots[s].transform.position, Input.mousePosition) <= 65)
                return items[s];
        }

        return null;
    }
    
    public GameObject FindClosestSlotObject()
    {
        for (int s = 0; s < slots.Length; s++)
        {
            if (Vector2.Distance(slots[s].transform.position, Input.mousePosition) <= 65)
                return slots[s];
        }

        return null;
    }
    
    public bool IsOverSlot()
    {
        for (int s = 0; s < slots.Length; s++)
        {
            if (Vector2.Distance(slots[s].transform.position, Input.mousePosition) <= 65)
                return true;
        }

        return false;
    }
    
    #endregion
}

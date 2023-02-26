using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Object[] crafts;
    
    
    public GameObject slotHolder;
    
    public SlotClass[] startingItems;
    public SlotClass[] items;
    //public List<SlotClass> equips = new List<SlotClass>();

    public GameObject[] slots;

    public int equippedToolIndex;
    public SlotClass equippedTool;

    [Header("Moving")] 
    public GameObject itemCursor;
    public SlotClass originalSlot;
    public SlotClass tempSlot;
    public SlotClass movingSlot;
    public bool isMovingItem;

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
        for (int s = 0; s < startingItems.Length; s++)
            Add(startingItems[s].item, startingItems[s].count);

        equippedToolIndex = items.Length - 1;
        Refresh();
    }

    public void Craft(CraftingRecipeClass craft)
    {
        if (craft.CanCraft(this))
        {
            craft.Craft(this);
        }
        else
        {
            Debug.Log("Cant craft lol adajsdjd");
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
            itemCursor.GetComponentInChildren<Image>().sprite = movingSlot.item.ItemSprite;
            itemCursor.GetComponentInChildren<Text>().text = movingSlot.count.ToString();
        }
            

        if (Input.GetMouseButtonDown(0))
        {
            ItemMoveOnLeftClick();
        }
        
        else if (Input.GetMouseButtonDown(1)) // right click
        {
            if (isMovingItem)
                PutSingle();
            else
                TakeHalf();
        }
        // if there is an item in tool slot
        if (items[equippedToolIndex].item != null)
        {
            // check if it isnt a tool
            if (items[equippedToolIndex].item.GetTool() == null)
            {
                // start moving item so it wouldnt be put in to a slot
                // fix a bug when you right click on tool slot the tool disappears
                ItemMoveOnLeftClick();
            }
        }

        if (items[equippedToolIndex].item != null)
            equippedTool = items[equippedToolIndex];
    }

    public bool isInvFull()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].item == null) return false;
        }
        return true;
    }

    public void ItemMoveOnLeftClick()
    {
        if (isMovingItem) // end item move
            EndItemMove();
        else
            BeginItemMove();
    }

    #region Item Stuff

    public void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            try
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = true;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = items[i].item.ItemSprite;
                if(items[i].item.isStackable)
                    slots[i].transform.GetChild(1).GetComponent<Text>().text = items[i].count.ToString();
                else
                    slots[i].transform.GetChild(1).GetComponent<Text>().text = "";
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

    public bool BeginItemMove()
    {
        originalSlot = FindClosestSlot();
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
        originalSlot = FindClosestSlot();
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
        originalSlot = FindClosestSlot();
        if (originalSlot == null)
        {
            Add(movingSlot.item, movingSlot.count);
            movingSlot.Clear();
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
        originalSlot = FindClosestSlot();
        
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
    
    public SlotClass FindClosestSlot()
    {
        for (int s = 0; s < slots.Length; s++)
        {
            if (Vector2.Distance(slots[s].transform.position, Input.mousePosition) <= 65)
                return items[s];
        }

        return null;
    }
    
    #endregion
}

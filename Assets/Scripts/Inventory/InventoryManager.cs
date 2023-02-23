using UnityEngine;
using UnityEngine.Serialization;
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

        for (int s = 0; s < startingItems.Length; s++)
            items[s] = startingItems[s];

        for (int i = 0; i < slotHolder.transform.childCount; i++)
            slots[i] = slotHolder.transform.GetChild(i).gameObject;

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
            itemCursor.GetComponent<Image>().sprite = movingSlot.GetItem().ItemSprite;

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
        if (items[equippedToolIndex].GetItem() != null)
        {
            // check if it isnt a tool
            if (items[equippedToolIndex].GetItem().GetTool() == null)
            {
                // start moving item so it wouldnt be put in to a slot
                ItemMoveOnLeftClick();
            }
        }

        if (items[equippedToolIndex].GetItem() != null)
            equippedTool = items[equippedToolIndex];
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
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = items[i].GetItem().ItemSprite;
                if(items[i].GetItem().isStackable)
                    slots[i].transform.GetChild(1).GetComponent<Text>().text = items[i].GetCount().ToString();
                else
                    slots[i].transform.GetChild(1).GetComponent<Text>().text = "";
            }
            catch
            {
                slots[i].transform.GetChild(0).GetComponent<Image>().enabled = false;
                slots[i].transform.GetChild(0).GetComponent<Image>().sprite = null;
                slots[i].transform.GetChild(1).GetComponent<Text>().text = "";
            }
        }
    }
    
    public bool Add(ItemClass item, int count)
    {
        //items.Add(item);
        SlotClass slot = Contains(item);
        
        if (slot != null && slot.GetItem().isStackable)
        {
            slot.AddCount(1);
        }
        else
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i].GetItem() == null) // empty slot
                {
                    items[i].AddItem(item, count);
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
            if(temp.GetCount() > 1)
                temp.SubCount(count);
            else
            {
                int removeSlotIndex = 0;
                for(int index = 0; index < items.Length; index++)
                {
                    if (items[index].GetItem() == item)
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
            if (items[i].GetItem() == item)
                return items[i];
        }

        return null;
    }
    
    public bool ContainsBool(ItemClass item, int count)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i].GetItem() == item && items[i].GetCount() >= count)
                return true;
        }

        return false;
    }
    
    #endregion

    #region Moving Stuff

    public bool BeginItemMove()
    {
        originalSlot = FindClosestSlot();
        if (originalSlot == null || originalSlot.GetItem() == null)
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
        if (originalSlot == null || originalSlot.GetItem() == null)
            return false; // no item to take half from

        movingSlot = new SlotClass(originalSlot.GetItem(), Mathf.CeilToInt(originalSlot.GetCount() / 2f)); // setting the moving slot to item we clicked on but with half count
        originalSlot.SubCount(Mathf.CeilToInt(originalSlot.GetCount() / 2f)); // taking half from orig slot
        if(originalSlot.GetCount() < 1)
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
            Add(movingSlot.GetItem(), movingSlot.GetCount());
            movingSlot.Clear();
        }
        else
        {
            if (originalSlot.GetItem() != null) // slot isnt empty
            {
                if (originalSlot.GetItem() == movingSlot.GetItem()) // same items
                {
                    if (originalSlot.GetItem().isStackable)
                    {
                        originalSlot.AddCount(movingSlot.GetCount());
                        movingSlot.Clear();
                    }
                    else
                        return false;
                }
                else
                {
                    tempSlot = new SlotClass(originalSlot);
                    originalSlot.AddItem(movingSlot.GetItem(), movingSlot.GetCount());
                    movingSlot.AddItem(tempSlot.GetItem(), tempSlot.GetCount());
                    Refresh();
                    return true;
                }
            }
            else // place item as usual
            {
                originalSlot.AddItem(movingSlot.GetItem(), movingSlot.GetCount());
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
        
        movingSlot.SubCount(1);
        if (originalSlot.GetItem() != null && originalSlot.GetItem() == movingSlot.GetItem())
            originalSlot.AddCount(1);
        else
            originalSlot.AddItem(movingSlot.GetItem(), 1);    
        
        if (movingSlot.GetCount() < 1)
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

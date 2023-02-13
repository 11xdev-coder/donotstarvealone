using System.Collections;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SlotClass
{
    public ItemClass item;
    public int count;
    
    public SlotClass()
    {
        item = null;
        count = 0;
    }

    public SlotClass(ItemClass _item, int _count)
    {
        item = _item;
        count = _count;
    }

    public SlotClass(SlotClass slot)
    {
        item = slot.GetItem();
        count = slot.GetCount();
    }

    public ItemClass GetItem()
    {
        return item; 
    }
    
    public int GetCount()
    {
        return count; 
    }
    
    public void AddCount(int add)
    {
        count += add; 
    }
    
    public void SubCount(int remove)
    {
        count -= remove; 
    }

    public void Clear()
    {
        this.item = null;
        this.count = 0;
    }

    public void AddItem(ItemClass item, int count)
    {
        this.item = item;
        this.count = count;
    }
}

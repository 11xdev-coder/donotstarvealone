using System.Collections;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class SlotClass
{
    [field: SerializeField] public ItemClass item { get; private set;  } = null;
    [field: SerializeField] public int count { get; private set; } = 0;

    //public SlotType slotType;
    
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
        item = slot.item;
        count = slot.count;
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

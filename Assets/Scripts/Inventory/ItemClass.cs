using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item")]
    public string ItemName;
    public Sprite ItemSprite;
    public bool isStackable;
    public int maxStack = 20;

    public virtual void Use(playerController caller)
    {
        
    }
    
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

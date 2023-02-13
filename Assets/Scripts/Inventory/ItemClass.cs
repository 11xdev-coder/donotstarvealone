using UnityEngine;

public abstract class ItemClass : ScriptableObject
{
    [Header("Item")]
    public string ItemName;
    public Sprite ItemSprite;
    public bool isStackable;

    public abstract ItemClass GetItem();
    public abstract ToolClass GetTool();
    public abstract MiscClass GetMisc();
    public abstract ConsumableClass GetConsumable();
}

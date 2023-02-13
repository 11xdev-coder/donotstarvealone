using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Item/Tool")]
public class ToolClass : ItemClass
{
    public ToolType toolType;

    public enum ToolType
    {
        Weapon,
        Pickaxe,
        Axe,
        Hammer
    }
    
    public override ItemClass GetItem() { return this; }
    public override ToolClass GetTool() { return this; }
    public override MiscClass GetMisc() { return null; }
    public override ConsumableClass GetConsumable() { return null; }
}

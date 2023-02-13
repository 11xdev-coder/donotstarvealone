using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Item/Consumable")]
public class ConsumableClass : ItemClass
{
    public float heals;
    
    public override ItemClass GetItem() { return this; }
    public override ToolClass GetTool() { return null; }
    public override MiscClass GetMisc() { return null; }
    public override ConsumableClass GetConsumable() { return this; }
}

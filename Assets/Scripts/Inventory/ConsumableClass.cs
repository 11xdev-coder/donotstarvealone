using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Item/Consumable")]
public class ConsumableClass : ItemClass
{
    public float heals;
    
    public override ConsumableClass GetConsumable() { return this; }

    protected override void Use(PlayerController caller)
    {
        base.Use(caller);
        Debug.Log("Ate");
        caller.inventory.UseSelected(this);
    }
}

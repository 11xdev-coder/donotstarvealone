using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Item/Tool/Fire-inflict weapon")]
public class FireInflictWeapon : ToolClass
{
    protected override void Use(PlayerController caller)
    {
        base.Use(caller);
        Debug.Log("fire inflict ig");
    }
}

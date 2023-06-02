using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Item/Tool/Fire-inflict weapon")]
public class FireInflictWeapon : ToolClass
{
    public override void Use(PlayerController caller)
    {
        base.Use(caller);
        Debug.Log("fire inflict ig");
    }
}

using System.Runtime.InteropServices;
using UnityEngine;

[CreateAssetMenu(fileName = "New Tool", menuName = "Item/Tool/Tool Base")]
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

    public override void Use(PlayerController caller)
    {
        base.Use(caller);
        Debug.Log("Swing");
    }
    public override ToolClass GetTool() { return this; }
}

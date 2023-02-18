using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Misc", menuName = "Item/Misc")]
public class MiscClass : ItemClass
{
    public override MiscClass GetMisc() { return this; }
    public override void Use(playerController caller) { }
}

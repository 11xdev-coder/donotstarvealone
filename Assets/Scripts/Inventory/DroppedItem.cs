using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroppedItem : MonoBehaviour
{
    public ItemClass item;
    public int count;
    public Vector3 pickupOffset;

    public void Initialize(ItemClass item, int count)
    {
        this.item = item;
        this.count = count;
    }
}

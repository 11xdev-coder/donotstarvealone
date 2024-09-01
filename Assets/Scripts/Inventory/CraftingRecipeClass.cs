using Inventory;
using UnityEngine;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipeClass : ScriptableObject
{
    [Header("Craft Items")] 
    public SlotClass[] inputItems;
    public SlotClass outputItem;

    public bool CanCraft(InventoryManager inv)
    {
        if (inv == null || ItemRegistry.Instance == null)
        {
            Debug.LogWarning("Invalid inventory or ItemRegistry.");
            return false;
        }

        if (inv.IsInventoryFull())
        {
            Debug.Log("Inventory is full.");
            return false;
        }

        foreach (var inputItem in inputItems)
        {
            if (inputItem?.item == null)
            {
                Debug.LogWarning("Null input item in recipe.");
                return false;
            }

            if (!inv.ContainsBool(inputItem.item, inputItem.count))
            {
                Debug.Log($"Missing item: {inputItem.item.itemName}, Required: {inputItem.count}");
                return false;
            }
        }

        Debug.Log("Can craft: All conditions met.");
        return true;
    }
    public void Craft(InventoryManager inv)
    {
        if (!inv.isLocalPlayer || ItemRegistry.Instance == null) return;

        foreach (var inputItem in inputItems)
        {
            if (inputItem?.item != null)
            {
                int itemId = ItemRegistry.Instance.GetIdByItem(inputItem.item);
                if (itemId != 0)  // Assuming 0 is an invalid ID
                {
                    inv.CmdRemoveItem(itemId, inputItem.count);
                }
                else
                {
                    Debug.LogWarning($"Invalid item ID for {inputItem.item.itemName} in crafting recipe.");
                    return;  // Exit if we encounter an invalid item
                }
            }
            else
            {
                Debug.LogWarning("Null input item in crafting recipe.");
                return;  // Exit if we encounter a null item
            }
        }

        if (outputItem?.item != null)
        {
            int outputItemId = ItemRegistry.Instance.GetIdByItem(outputItem.item);
            if (outputItemId != 0)  // Assuming 0 is an invalid ID
            {
                inv.RequestAddItem(outputItemId, outputItem.count);
            }
            else
            {
                Debug.LogWarning($"Invalid item ID for output item {outputItem.item.itemName} in crafting recipe.");
            }
        }
        else
        {
            Debug.LogWarning("Null output item in crafting recipe.");
        }
    }
}
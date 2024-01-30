
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
        if (inv.IsInventoryFull()) return false;
        
        for (int i = 0; i < inputItems.Length; i++)
        {
            if (!inv.ContainsBool(inputItems[i].item, inputItems[i].count))
            {
                return false;
            }
        }
        return true;
    }

    public void Craft(InventoryManager inv)
    {
        if (inv.isLocalPlayer)
        {
            for (int i = 0; i < inputItems.Length; i++)
            {
                inv.CmdRemoveItem(ItemRegistry.Instance.GetIdByItem(inputItems[i].item), inputItems[i].count);
            }

            inv.RequestAddItem(ItemRegistry.Instance.GetIdByItem(outputItem.item), outputItem.count);
        }
    }
}

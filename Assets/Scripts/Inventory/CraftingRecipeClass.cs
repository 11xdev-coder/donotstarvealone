
using UnityEngine;

[CreateAssetMenu(fileName = "New Crafting Recipe", menuName = "Crafting/Recipe")]
public class CraftingRecipeClass : ScriptableObject
{
    [Header("Craft Items")] 
    public SlotClass[] inputItems;
    public SlotClass outputItem;
    

    public bool CanCraft(InventoryManager inv)
    {
        if (inv.isInvFull()) return false;
        
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
        for (int i = 0; i < inputItems.Length; i++)
        {
            inv.Remove(inputItems[i].item, inputItems[i].count);
        }

        inv.Add(outputItem.item, outputItem.count);
    }
}

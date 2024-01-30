using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Inventory
{
    public class ItemRegistry : MonoBehaviour
    {
        [System.Serializable]
        public class ItemEntry
        {
            public int id;
            public ItemClass item;
        }
    
        public List<ItemEntry> itemEntries; 

        private Dictionary<int, ItemClass> itemById;
    
        public static ItemRegistry Instance { get; private set; }
    
        void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("Multiple instances of ItemRegistry found!");
                return;
            }
    
            Instance = this;
        
            itemById = new Dictionary<int, ItemClass>();
            foreach (var entry in itemEntries)
            {
                itemById.Add(entry.id, entry.item);
            }
        }

        public ItemClass GetItemById(int id)
        {
            if (itemById.TryGetValue(id, out ItemClass obj))
            {
                return obj;
            }
            else
            {
                Debug.LogWarning($"Item with ID {id} not found.");
                return null;
            }
        }
        
        public int GetIdByItem(ItemClass item)
        {
            foreach (var entry in itemById)
            {
                if (entry.Value == item)
                {
                    return entry.Key;
                }
            }
            
            Debug.LogWarning($"ID {item.itemId} not found in registry.");
            return 0;
        }
    }
}
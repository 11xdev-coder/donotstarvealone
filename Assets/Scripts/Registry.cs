using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Registry : MonoBehaviour
{
    [System.Serializable]
    public class ObjectEntry
    {
        public string id;
        public GameObject prefab;
    }
    
    public List<ObjectEntry> objectEntries; // Populate this in the Inspector with all your object prefabs
    public List<Tile> tileTypes; // Populate this with all your tile types

    private Dictionary<string, GameObject> objectById;
    private Dictionary<string, Tile> tileById;
    
    public static Registry Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning("Multiple instances of Registry found!");
            return;
        }
    
        Instance = this;
        
        objectById = new Dictionary<string, GameObject>();
        foreach (var entry in objectEntries)
        {
            objectById.Add(entry.id, entry.prefab);
        }
        tileById = tileTypes.ToDictionary(tile => tile.name, tile => tile);
    }

    public GameObject GetObjectById(string id)
    {
        if (objectById.TryGetValue(id, out GameObject obj))
        {
            return obj;
        }
        else
        {
            Debug.LogWarning($"Object with ID {id} not found.");
            return null;
        }
    }

    public Tile GetTileById(string id)
    {
        if (tileById.TryGetValue(id, out Tile tile))
        {
            return tile;
        }
        else
        {
            Debug.LogWarning($"Tile with ID {id} not found.");
            return null;
        }
    }
}
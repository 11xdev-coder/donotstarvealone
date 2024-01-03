using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Biome", menuName = "Biome", order = 1)]
public class Biome : ScriptableObject
{
    public Tile mainTile;
    public bool canPlayerSpawn;
    public float freqToSpawn;
    public WorldGenerator.PossibleBiomes possibleBiome;
    public AudioClip music;
    
    [Header("Objects")]
    public List<GameObject> objects;
    public float[] spawnChances;
    
    [Header("Island")]
    public bool isIsland;
    public int numberOfIslands;
    public int islandRadius;
    public int distanceFromMainIsland;

    [Header("Beach for Island")] 
    public bool doGenerateBeach;
    public Tile beachTile;
    public int beachWidth;

    /// <summary>
    /// 
    /// </summary>
    /// <returns>A random object from the objects list including all the chances</returns>
    public GameObject GetRandomObject()
    {
        if (objects.Count == 0 || objects.Count != spawnChances.Length)
            return null;

        if (objects.Count == 1 && Random.Range(0f, 1f) < spawnChances[0]) // only one object
        {
            return objects[0]; // return it without any loops
        }

        for (int i = 0; i < objects.Count; i++)
        {
            float spawnChance = Random.Range(0f, 1f);
            if (spawnChance < spawnChances[i])
            {
                return objects[i];
            }
        }

        return null;
    }
}
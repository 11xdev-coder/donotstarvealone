using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class worldgenerator : MonoBehaviour
{
    [Header("world settings")]
    public int width = 1000;
    public int height = 1000;
    public int freq = 2;
    public int partToSpawnPlayer = 2;
    public bool havePlayerSpawned = false;
    public Vector3 treeOffset = new Vector3(0, 0.7f, 0);
    
    
    [Header("tile prefabs")]
    public GameObject grassTurf;
    public GameObject forestTurf;
    public GameObject rockylandTurf;
    public GameObject ocean;

    [Header("objects")] 
    public GameObject player;
    public GameObject birchnutTree;
    public GameObject pineTree;
    public GameObject miniBoulder;
    public GameObject rockOcean;

    private Dictionary<string, GameObject[]> objectsByBiome;
    private float[,] noiseValues;
    private Dictionary<string, Dictionary<string, float>> objectSpawnChancesByBiome;
    
    public void Awake()
    {
        objectsByBiome = new Dictionary<string, GameObject[]>()
        {
            {"Grassland", new GameObject[] {birchnutTree}},
            {"Forest", new GameObject[] {pineTree, miniBoulder}},
            {"Rockyland", new GameObject[] {miniBoulder}},
            {"Ocean", new GameObject[] {rockOcean}}
        };
        objectSpawnChancesByBiome = new Dictionary<string, Dictionary<string, float>>()
        {
            {
                "Grassland",
                new Dictionary<string, float>()
                {
                    {"birchnutTree", 0.07f}
                }
            },
            {
                "Forest",
                new Dictionary<string, float>()
                {
                    {"pineTree", 0.1f},
                    {"miniBoulder", 0.05f}
                }
            },
            {
                "Rockyland",
                new Dictionary<string, float>()
                {
                    {"miniBoulder", 0.075f}
                }
            },
            {
                "Ocean",
                new Dictionary<string, float>()
                {
                    {"rockOcean", 0.01f}
                }
            }
        };
        
        noiseValues = new float[width, height];
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                noiseValues[x, y] = Mathf.PerlinNoise((float)x / width * freq, (float)y / height * freq);
            }
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = noiseValues[x, y];
                GameObject terrain;
                GameObject[] objects;
                string biome;

                if (noiseValue > 0.5f)
                {
                    if (noiseValue > 0.6f)
                    {
                        if (noiseValue >= 0.7f)
                        {
                            terrain = rockylandTurf;
                            objects = objectsByBiome["Rockyland"];
                            biome = "Rockyland";
                        }
                        else
                        {
                            terrain = forestTurf;
                            objects = objectsByBiome["Forest"];
                            biome = "Forest";
                        }
                    }
                    else
                    {
                        if (x > width / partToSpawnPlayer && y > height / partToSpawnPlayer && !havePlayerSpawned)
                        {
                            player.transform.position = new Vector3(x, y, -1);
                            havePlayerSpawned = true;
                        }
                            
                        terrain = grassTurf;
                        objects = objectsByBiome["Grassland"];
                        biome = "Grassland";
                    }
                }
                else
                {
                    terrain = ocean;
                    objects = objectsByBiome["Ocean"];
                    biome = "Ocean";
                }

                Vector3 position = new Vector3(x, y, 0);
                Instantiate(terrain, position, Quaternion.identity);
                
                float spawnChance = Random.Range(0f, 1f);
                foreach (string objectType in objectSpawnChancesByBiome[biome].Keys)
                {
                    if (spawnChance < objectSpawnChancesByBiome[biome][objectType])
                    {
                        Vector3 objectPosition;
                        int objectIndex = Random.Range(0, objects.Length);
                        GameObject objectToInstantiate = objects[objectIndex];
                        if (objectToInstantiate.name.Contains(objectType))
                        {
                            objectToInstantiate.GetComponent<SpriteRenderer>().sortingOrder = height - y;
                            if (objectToInstantiate.name.Contains("Tree"))
                            {
                                objectPosition = new Vector3(x, y, -0.3f) + treeOffset;
                                Instantiate(objectToInstantiate, objectPosition, Quaternion.identity);
                            }
                            else
                            {
                                objectPosition = new Vector3(x, y, -0.3f);
                                Instantiate(objectToInstantiate, objectPosition, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }
    }
}

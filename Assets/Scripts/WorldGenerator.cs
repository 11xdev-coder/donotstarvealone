using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldGenerator : MonoBehaviour
{
    [Header("world settings")]
    public int width = 1000;
    public int height = 1000;
    public int freq = 2;
    public int partToSpawnPlayer = 2;
    public bool havePlayerSpawned;
    
    
    [Header("Tilemaps")]
    public Tilemap terrainTilemap;

    [Header("Tiles")]
    public Tile grassTile;
    public Tile forestTile;
    public Tile rockylandTile;
    public Tile oceanTile;

    [Header("objects")] 
    public GameObject player;
    public GameObject birchnutTree;
    public GameObject pineTree;
    public GameObject miniBoulder;
    public GameObject rockOcean;
    public GameObject boulder;

    private Dictionary<string, GameObject[]> _objectsByBiome;
    private float[,] _noiseValues;
    private Dictionary<string, Dictionary<string, float>> _objectSpawnChancesByBiome;
    
    public void Awake()
    {
        _objectsByBiome = new Dictionary<string, GameObject[]>
        {
            {"Grassland", new [] {birchnutTree}},
            {"Forest", new [] {pineTree, miniBoulder}},
            {"Rockyland", new [] {miniBoulder, boulder}},
            {"Ocean", new [] {rockOcean}}
        };
        _objectSpawnChancesByBiome = new Dictionary<string, Dictionary<string, float>>()
        {
            {
                "Grassland",
                new Dictionary<string, float>()
                {
                    {"BirchNutTree", 0.07f}
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
                    {"miniBoulder", 0.075f},
                    {"boulder", 0.005f}
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
        
        _noiseValues = new float[width, height];
    }

    // Start is called before the first frame update
    void Start()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _noiseValues[x, y] = Mathf.PerlinNoise((float)x / width * freq, (float)y / height * freq);
            }
        }
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = _noiseValues[x, y];
                Tile terrain;
                GameObject[] objects;
                string biome;

                if (noiseValue > 0.5f)
                {
                    if (noiseValue > 0.6f)
                    {
                        if (noiseValue >= 0.7f)
                        {
                            terrain = rockylandTile;
                            objects = _objectsByBiome["Rockyland"];
                            biome = "Rockyland";
                        }
                        else
                        {
                            terrain = forestTile;
                            objects = _objectsByBiome["Forest"];
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
                            
                        terrain = grassTile;
                        objects = _objectsByBiome["Grassland"];
                        biome = "Grassland";
                    }
                }
                else
                {
                    terrain = oceanTile;
                    objects = _objectsByBiome["Ocean"];
                    biome = "Ocean";
                }

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                terrainTilemap.SetTile(tilePosition, terrain);
                
                
                float spawnChance = Random.Range(0f, 1f);
                foreach (string objectType in _objectSpawnChancesByBiome[biome].Keys)
                {
                    if (spawnChance < _objectSpawnChancesByBiome[biome][objectType])
                    {
                        Vector3 objectPosition;
                        int objectIndex = Random.Range(0, objects.Length);
                        GameObject objectToInstantiate = objects[objectIndex];
                        if (objectToInstantiate.name.Contains(objectType))
                        {
                            GameObject instantiatedObject; 
                            
                            objectPosition = new Vector3(x, y, 0);
                            instantiatedObject = Instantiate(objectToInstantiate, objectPosition, Quaternion.identity); 

                            float baseOrder = width - objectPosition.y + 1;  // Use objectPosition.y instead of transform.position.y
                            if (instantiatedObject.GetComponent<SpriteRenderer>() != null)
                            {
                                instantiatedObject.GetComponent<SpriteRenderer>().sortingOrder = (int)baseOrder; 
                            }

                            if (instantiatedObject.GetComponentInChildren<SpriteRenderer>() != null)
                            {
                                instantiatedObject.GetComponentInChildren<SpriteRenderer>().sortingOrder = (int)baseOrder; 
                            }
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldGenerator : NetworkBehaviour
{
    [Serializable]
    public class WorldData
    {
        public List<TileData> tileData;
        public List<TileData> collidableTileData;
        public List<GameObjectData> gameObjectData;
    }
    
    [Serializable]
    public class TileData
    {
        public string tileId;
        public Vector3Int position;
    }
    
    [Serializable]
    public class GameObjectData
    {
        public string objectId;
        public Vector3Int position;
    }

    public Dictionary<Vector3Int, GameObjectData> objects;
    public List<HashSet<Vector3Int>> islands = new List<HashSet<Vector3Int>>();
    [SyncVar] public float globalTicks;
    
    [Header("Seed")] 
    public int seed;
    
    [Header("World Settings")]
    public int width = 1000;
    public int height = 1000;
    public int freq = 2;
    public int partToSpawnPlayer = 2;
    public bool havePlayerSpawned;
    
    [Header("Tilemaps")]
    public Tilemap triggerTilemap;
    public Tilemap collidableTilemap;

    [Header("Biomes")] 
    public List<Biome> biomes;
    // public Biome grasslandBiome;
    // public Biome forestBiome;
    // public Biome rockylandBiome;
    public Biome oceanBiome;

    public enum PossibleBiomes
    {
        Ash,
        Rocky,
        Ocean,
        Grass,
        Forest
    }

    [Header("Spawn")] 
    [SyncVar] public Vector3Int spawnPoint;

    private Vector2 _perlinOffset;
    private Registry _registry;
    private List<byte> _receivedChunks = new List<byte>();
    private const int MaxChunkSize = 1000;
    
    public static WorldGenerator Instance { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GenerateAndInitializeWorldForHost();
    }
    
    void Awake()
    {
        // Check if Instance already exists and warn if there are multiple instances
        if (Instance != null)
        {
            Debug.LogWarning("Multiple instances of WorldGenerator found!");
            return;
        }

        // Initialize the instance
        Instance = this;

        DontDestroyOnLoad(gameObject);
        
        Random.InitState(seed);
        _perlinOffset = new Vector2(Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));
        _registry = Registry.Instance;
        objects = new Dictionary<Vector3Int, GameObjectData>();
    }
    
    public void GenerateAndInitializeWorldForHost()
    {
        GenerateEverything(); // Your existing world generation logic

        // Serialize the current world state
        byte[] worldData = SerializeWorldData();

        // Send this data to all clients (existing and future ones)
        RpcSendWorldDataToClients(worldData);
    }

    [ClientRpc]
    void RpcSendWorldDataToClients(byte[] worldData)
    {
        // If this is the host itself, no need to deserialize again
        if (isServer) return;

        // Clients deserialize the world state
        DeserializeWorldState(worldData);
    }
    
    [TargetRpc]
    public void TargetSendWorldDataToClient(NetworkConnection target, byte[] worldData)
    {
        // Client deserializes the world state
        DeserializeWorldState(worldData);
    }

    public void GenerateEverything()
    {
        GenerateWorld();
        foreach (Biome biome in biomes)
        {
            if (biome.isIsland)
            {
                for (var i = 0; i < biome.numberOfIslands; i++)
                {
                    GenerateIsland(biome, biome.islandRadius);
                }
            }
        }
        
        DetectIslands();
    }

    void Update()
    {
        globalTicks += Time.deltaTime;
    }
    
    /// <summary>
    /// Generates non-island biomes (all of them at once)
    /// </summary>
    void GenerateWorld()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noiseValue = Mathf.PerlinNoise((float)x / width * freq + _perlinOffset.x, 
                    (float)y / height * freq + _perlinOffset.y);
                Biome currentBiome = DetermineBiome(noiseValue);
                Tilemap currentTilemap = currentBiome == oceanBiome ? collidableTilemap : triggerTilemap;

                if (ShouldSpawnPlayer(x, y, currentBiome))
                {
                    spawnPoint = new Vector3Int(x, y, 0);
                    havePlayerSpawned = true;
                }

                Vector3Int tilePosition = new Vector3Int(x, y, 0);
                currentTilemap.SetTile(tilePosition, currentBiome.mainTile);

                TrySpawnObject(currentBiome, x, y);
            }
        }
    }
    
    /// <summary>
    /// Generates island biome (only one!)
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="baseRadius"></param>
    private void GenerateIsland(Biome biome, int baseRadius)
    {
        Vector3Int centerPosition = FindRandomPositionNearCenter();
        float noiseScale = 0.3f; // island noise affection scale

        // noise to randomize island shape
        Vector2 noiseOffset = new Vector2(Random.Range(0f, 100f), Random.Range(0f, 100f));

        // list to store positions that werent filled
        List<Vector3Int> skippedPositions = new List<Vector3Int>();

        for (int x = -baseRadius; x <= baseRadius; x++)
        {
            for (int y = -baseRadius; y <= baseRadius; y++)
            {
                Vector3Int tilePosition = new Vector3Int(centerPosition.x + x, centerPosition.y + y, 0);

                // calculate noise
                float noise = Mathf.PerlinNoise((x + noiseOffset.x) * noiseScale, (y + noiseOffset.y) * noiseScale);
                float radius = baseRadius * (0.8f + 0.4f * noise); // less/more roundness

                if (Vector3Int.Distance(centerPosition, tilePosition) <= radius)
                {
                    triggerTilemap.SetTile(tilePosition, biome.mainTile);
                    TrySpawnObject(biome, tilePosition.x, tilePosition.y);
                    
                    RemoveOceanTile(tilePosition);
                    PlaceSurroundingOceanTiles(tilePosition, biome);
                }
            }
        }

        // fill skipped tiles
        /*foreach (Vector3Int pos in skippedPositions)
        {
            if (Random.value < 0.1f) // 0.1f - jaggedness
            {
                // if has enough neighbors - fill that hole
                if (HasEnoughNeighbors(pos, skippedPositions))
                {
                    triggerTilemap.SetTile(pos, biome.mainTile);
                    RemoveSpawnedObject(pos);
                }
            }
        }*/
        
        CreateBufferZoneAroundIsland(centerPosition, baseRadius, biome);
    }
    
    
    /// <summary>
    /// Creates an ocean around an island (bufferZoneRadius = Biome's param)
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="baseRadius"></param>
    /// <param name="biome"></param>
    private void CreateBufferZoneAroundIsland(Vector3Int centerPosition, int baseRadius, Biome biome)
    {
        int bufferZoneRadius = biome.distanceFromMainIsland;
        int extendedRadius = baseRadius + (biome.doGenerateBeach ? biome.beachWidth : 0) + bufferZoneRadius;

        for (int x = -extendedRadius; x <= extendedRadius; x++)
        {
            for (int y = -extendedRadius; y <= extendedRadius; y++)
            {
                Vector3Int tilePosition = new Vector3Int(centerPosition.x + x, centerPosition.y + y, 0);
                float distanceFromCenter = Vector3Int.Distance(centerPosition, tilePosition);
                
                // generate ocean buffer zone
                if (distanceFromCenter > baseRadius && distanceFromCenter <= baseRadius + bufferZoneRadius)
                {
                    if (triggerTilemap.GetTile(tilePosition) != biome.mainTile)
                    {
                        SwapForOceanTile(tilePosition, biome);
                    }
                }
                
                // generate beach if within beach width
                if (biome.doGenerateBeach && distanceFromCenter > baseRadius + bufferZoneRadius && 
                    distanceFromCenter <= baseRadius + bufferZoneRadius+ biome.beachWidth)
                {
                    // if not biome's main tile and doesn't have any ocean tile (collidable)
                    if (triggerTilemap.GetTile(tilePosition) != biome.mainTile && !collidableTilemap.HasTile(tilePosition))
                    {
                        triggerTilemap.SetTile(tilePosition, biome.beachTile);
                    }
                }
                
            }
        }
    }


    Vector3Int FindRandomPositionNearCenter()
    {
        int centerX = width / 2;
        int centerY = height / 2;
        int range = Mathf.Min(width, height) / 4; // Adjust range for "closeness" to center

        int x = Random.Range(centerX - range, centerX + range);
        int y = Random.Range(centerY - range, centerY + range);
        return new Vector3Int(x, y, 0);
    }
    
    /// <summary>
    /// Gets random position from the tile
    /// </summary>
    /// <param name="placedTiles"></param>
    /// <returns>Random position</returns>
    Vector3Int GetRandomAdjacentPosition(HashSet<Vector3Int> placedTiles)
    {
        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        Vector3Int randomTile = new List<Vector3Int>(placedTiles)[Random.Range(0, placedTiles.Count)];
        Vector3Int randomOffset = offsets[Random.Range(0, offsets.Length)];
        return randomTile + randomOffset;
    }
    
    /// <summary>
    /// Places 4 ocean tiles around the tile
    /// </summary>
    /// <param name="position"></param>
    /// <param name="currentBiome"></param>
    void PlaceSurroundingOceanTiles(Vector3Int position, Biome currentBiome)
    {
        Vector3Int[] neighbors = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var offset in neighbors)
        {
            Vector3Int neighborPos = position + offset;
            if (triggerTilemap.GetTile(neighborPos) != currentBiome.mainTile)
            {
                SwapForOceanTile(neighborPos, currentBiome); // remove the normal tile and place ocean one
            }
        }
    }
    
    /// <summary>
    /// Checks if a tile has at least 3 tile neighbors around it
    /// </summary>
    /// <param name="position"></param>
    /// <param name="tiles"></param>
    /// <returns>True if >= 3 neighbors</returns>
    private bool HasEnoughNeighbors(Vector3Int position, List<Vector3Int> tiles)
    {
        int neighborCount = 0;
        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        foreach (var offset in offsets)
        {
            if (tiles.Contains(position + offset))
            {
                neighborCount++;
            }
        }
        
        return neighborCount >= 3;
    }

    private void RemoveSpawnedObject(Vector3Int tilePos)
    {
        if (objects.ContainsKey(tilePos))
        {
            //Destroy(objects[tilePos]);
            objects.Remove(tilePos);
        }
    }

    Biome DetermineBiome(float noiseValue)
    {
        foreach (Biome biome in biomes)
        {
            if (noiseValue > biome.freqToSpawn && !biome.isIsland) return biome;
        }
        return oceanBiome;
        // if (noiseValue > 0.7f) return rockylandBiome;
        // if (noiseValue > 0.6f) return forestBiome;
        // if (noiseValue > 0.5f) return grasslandBiome;
        // return oceanBiome;
    }

    bool ShouldSpawnPlayer(int x, int y, Biome currentBiome)
    {
        return x > width / partToSpawnPlayer && y > height / partToSpawnPlayer && 
               !havePlayerSpawned && currentBiome.canPlayerSpawn;
    }

    void TrySpawnObject(Biome biome, int x, int y)
    {
        string objectId = biome.GetRandomObjectId();
        if (objectId != null)
        {
            //Vector3 objectPosition = new Vector3(x, y, 0);
            //GameObject instantiatedObject = Instantiate(objectToInstantiate, objectPosition, Quaternion.identity);
            //SetSortingOrder(instantiatedObject, width - y + 1);
            
            // record object
            Vector3Int tilePos = new Vector3Int(x, y, 0);
            objects[tilePos] = new GameObjectData { objectId = objectId, position = tilePos };
        }
    }

    void SetSortingOrder(GameObject obj, int order)
    {
        SpriteRenderer spriteRenderer = obj.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = order;
        }
    }
    
    /// <summary>
    /// Removes an ocean tile
    /// </summary>
    /// <param name="pos"></param>
    private void RemoveOceanTile(Vector3Int pos) 
    {
        collidableTilemap.SetTile(pos, null); // remove ocean tile
    }
    
    /// <summary>
    /// Swaps a normal tile for an ocean one, deleting the object that was standing on this tile
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="currentBiome"></param>
    private void SwapForOceanTile(Vector3Int pos, Biome currentBiome)
    {
        collidableTilemap.SetTile(pos, oceanBiome.mainTile); // Set the ocean tile
        triggerTilemap.SetTile(pos, null); // Remove the normal tile

        // Remove any objects that might be on this tile
        if (objects.TryGetValue(pos, out var o))
        {
            if (currentBiome.objects.Contains(new BiomeObject { objectId = o.objectId})) return; // does not clear any objects that correspond to current biome
            RemoveSpawnedObject(pos);
        }
    }
    
    public void DetectIslands()
    {
        islands.Clear();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        
        BoundsInt bounds = triggerTilemap.cellBounds;
        TileBase[] allLandTiles = triggerTilemap.GetTilesBlock(bounds);

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = allLandTiles[x + y * bounds.size.x];
                if (tile != null) // We found a land tile
                {
                    Vector3Int tilePos = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                    if (!visited.Contains(tilePos))
                    {
                        HashSet<Vector3Int> newIsland = new HashSet<Vector3Int>();
                        FloodFill(tilePos, ref visited, ref newIsland);
                        if (newIsland.Count > 0)
                        {
                            islands.Add(newIsland);
                        }
                    }
                }
            }
        }

        // Debug output
        foreach (var island in islands)
        {
            Debug.Log($"Detected an island with {island.Count} tiles.");
        }
    }
    
    /// <summary>
    /// Flood fill algorithm
    /// </summary>
    /// <param name="start">
    /// Starting position
    /// </param>
    /// <param name="visited">
    /// Visited tiles
    /// </param>
    /// <param name="islandTiles">
    /// Detected island tiles
    /// </param>
    private void FloodFill(Vector3Int start, ref HashSet<Vector3Int> visited, ref HashSet<Vector3Int> islandTiles)
    {
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector3Int position = queue.Dequeue();

            // skip if the tile is visited or is water
            if (visited.Contains(position) || collidableTilemap.HasTile(position))
                continue;

            // mark the tiles as visited (they are island tiles)
            visited.Add(position);
            islandTiles.Add(position);

            // enqueue neighboring tiles
            EnqueueIfValid(position + Vector3Int.up, ref visited, queue);
            EnqueueIfValid(position + Vector3Int.down, ref visited, queue);
            EnqueueIfValid(position + Vector3Int.left, ref visited, queue);
            EnqueueIfValid(position + Vector3Int.right, ref visited, queue);
        }
    }
    
    /// <summary>
    /// Adds a tile to the queue if it hasn't been visited yet (helper method)
    /// </summary>
    /// <param name="position"></param>
    /// <param name="visited"></param>
    /// <param name="queue"></param>
    private void EnqueueIfValid(Vector3Int position, ref HashSet<Vector3Int> visited, Queue<Vector3Int> queue)
    {
        if (!visited.Contains(position) && triggerTilemap.HasTile(position) && !collidableTilemap.HasTile(position))
        {
            queue.Enqueue(position);
        }
    }
    
    public IEnumerator TransitionValues(float startValue, float endValue, float duration, Action<float> applyValue)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float currentValue = Mathf.Lerp(startValue, endValue, elapsed / duration);
            applyValue(currentValue);

            yield return null;
        }

        applyValue(endValue); 
    }

    
    public Vector3Int ClampVector3(Vector3 value)
    {
        return new Vector3Int(
            Mathf.RoundToInt(value.x),
            Mathf.RoundToInt(value.y),
            0
        );
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="position">Clamped to int automatically</param>
    /// <returns>A possible biome that is in the given position</returns>
    public PossibleBiomes GetBiomeByPositionEnum(Vector3 position)
    {
        Vector3Int intPosition = ClampVector3(position);
        TileBase tile = triggerTilemap.GetTile(intPosition);

        foreach (Biome biome in biomes)
        {
            if (biome.mainTile == tile)
            {
                return biome.possibleBiome;
            }
        }

        return PossibleBiomes.Ocean;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="position">Clamped to int automatically</param>
    /// <returns>A biome scriptable object that is in the given position</returns>
    public Biome GetBiomeByPosition(Vector3 position)
    {
        Vector3Int intPosition = ClampVector3(position);
        TileBase tile = triggerTilemap.GetTile(intPosition);

        foreach (Biome biome in biomes)
        {
            if (biome.mainTile == tile)
            {
                return biome;
            }
        }

        return oceanBiome;
    }
    
    public byte[] SerializeWorldData()
    {
        List<TileData> tileDataList = new List<TileData>();
        List<TileData> collidableTileDataList = new List<TileData>();
        List<GameObjectData> objectDataList = new List<GameObjectData>();

        // Serialize Tiles
        foreach (var position in triggerTilemap.cellBounds.allPositionsWithin)
        {
            var tile = triggerTilemap.GetTile<Tile>(position);
            if (tile != null)
            {
                tileDataList.Add(new TileData { position = position, tileId = tile.name });
            }
        }

        foreach (var position in collidableTilemap.cellBounds.allPositionsWithin)
        {
            var tile = collidableTilemap.GetTile<Tile>(position);
            if (tile != null)
            {
                collidableTileDataList.Add(new TileData { position = position, tileId = tile.name});
            }
        }

        // Serialize Objects
        foreach (var kvp in objects)
        {
            if (kvp.Value is {objectId: not null})
            {
                objectDataList.Add(new GameObjectData { position = kvp.Key, objectId = kvp.Value.objectId });
            }
        }


        // Convert to byte array
        string json = JsonUtility.ToJson(new WorldData { tileData = tileDataList, gameObjectData = objectDataList, collidableTileData = collidableTileDataList });
        return System.Text.Encoding.UTF8.GetBytes(json);
    }
    
    // Method to send data in chunks
    public void SendWorldDataInChunks(NetworkConnection conn, byte[] worldData)
    {
        for (int i = 0; i < worldData.Length; i += MaxChunkSize)
        {
            int chunkSize = Mathf.Min(MaxChunkSize, worldData.Length - i);
            byte[] chunk = new byte[chunkSize];
            Array.Copy(worldData, i, chunk, 0, chunkSize);
            TargetReceiveWorldChunk(conn, chunk, i == 0, i + chunkSize >= worldData.Length);
        }
    }
    
    
    [TargetRpc]
    private void TargetReceiveWorldChunk(NetworkConnection target, byte[] chunk, bool isFirstChunk, bool isLastChunk)
    {
        if (isFirstChunk)
        {
            _receivedChunks.Clear();
        }

        _receivedChunks.AddRange(chunk);

        if (isLastChunk)
        {
            byte[] fullData = _receivedChunks.ToArray();
            DeserializeWorldState(fullData);
        }
    }
    
    public void DeserializeWorldState(byte[] data)
    {
        string json = System.Text.Encoding.UTF8.GetString(data);
        WorldData worldData = JsonUtility.FromJson<WorldData>(json);
        
        // basic tiles
        foreach (var tileData in worldData.tileData)
        {
            Tile tile = _registry.GetTileById(tileData.tileId);
            triggerTilemap.SetTile(tileData.position, tile);
        }
        
        // ocean tiles (collidable)
        foreach (var tileData in worldData.collidableTileData)
        {
            Tile tile = _registry.GetTileById(tileData.tileId);
            collidableTilemap.SetTile(tileData.position, tile);
        }

        // Instantiate Objects
        foreach (var objectData in worldData.gameObjectData)
        {
            GameObject prefab = _registry.GetObjectById(objectData.objectId);
            if(isServer) 
            {
                GameObject go = Instantiate(prefab, objectData.position, Quaternion.identity);
                NetworkServer.Spawn(go);
                var position = go.transform.position;
                Debug.Log($"[Server] Spawned object {go.name} with netId: {go.GetComponent<NetworkIdentity>().netId} at {Time.time}" +
                          $"at x: {position.x} y: {position.y} z: {position.z}");
            }

        }
    }

}


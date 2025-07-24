using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        TextureManager.CreateTextures();

        SaveSystem.saveDataPath = Application.persistentDataPath; // Set the save data path
    }
    /// <summary>
    /// How far to render chunks horizontally (x and z dimensions) from the player.
    /// </summary>
    public int renderDistanceHorizontal = 5;

    /// <summary>
    /// How far to render chunks vertically (y dimension) from the player.
    /// </summary>
    public int renderDistanceVertical = 3;

    /// <summary>
    /// Prefab for the chunk to instantiate.
    /// </summary>
    public GameObject chunkPrefab;

    /// <summary>
    /// The chunks that are currently active in the world.
    /// </summary>
    public Dictionary<Vector3Int, Chunk> ActiveChunks = new();

    /// <summary>
    /// The chunks that are currently inactive and are waiting to be activated.
    /// </summary>
    public Queue<Chunk> InactiveChunks = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _ = StartCoroutine(ChunkUpdateLoop());
    }

    private void Update()
    {
        SaveSystem.Update(Time.deltaTime); // Update the save system timer
    }

    private void OnApplicationQuit()
    {
        SaveSystem.SaveAllChunksToDisk();
        running = false; // Stop the update loop when the application quits
    }

    public bool running = true;

    private readonly ConcurrentQueue<(Vector3Int pos, Block[,,] blocks)> readyChunks = new();
    [SerializeField] private int chunksPerFrame = 4;

    private IEnumerator ChunkUpdateLoop()
    {
        while (running)
        {
            Vector3Int playerChunkCoord = Vector3Int.FloorToInt(Player.Instance.transform.position / Chunk.CHUNK_SIZE);

            // Collect chunk positions within render distance
            List<Vector3Int> neededChunks = GetChunksInRange(playerChunkCoord);

            // Dispatch chunk generation/loading in parallel
            foreach (Vector3Int chunkPos in neededChunks)
            {
                if (!ActiveChunks.ContainsKey(chunkPos))
                {
                    Task.Run(() => LoadOrGenerateChunk(chunkPos));
                }
            }

            // Activate a limited number of ready chunks per frame
            int activatedThisFrame = 0;
            while (readyChunks.TryDequeue(out var data) && activatedThisFrame < chunksPerFrame)
            {
                ActivateChunk(data.pos, data.blocks);
                activatedThisFrame++;
            }

            // Unload distant chunks
            UnloadDistantChunks(playerChunkCoord);
            Debug.Log("Completed chunk update loop.");
            yield return null;
        }
    }

    private List<Vector3Int> GetChunksInRange(Vector3Int playerChunkCoord)
    {
        List<Vector3Int> neededChunks = new();

        for (int x = playerChunkCoord.x - renderDistanceHorizontal; x <= playerChunkCoord.x + renderDistanceHorizontal; x++)
        {
            for (int y = playerChunkCoord.y - renderDistanceVertical; y <= playerChunkCoord.y + renderDistanceVertical; y++)
            {
                for (int z = playerChunkCoord.z - renderDistanceHorizontal; z <= playerChunkCoord.z + renderDistanceHorizontal; z++)
                {
                    neededChunks.Add(new Vector3Int(x, y, z));
                }
            }
        }

        neededChunks.Sort((a, b) =>
        {
            int da = ManhattanDistance(a, playerChunkCoord);
            int db = ManhattanDistance(b, playerChunkCoord);
            return da.CompareTo(db);
        });

        return neededChunks;
    }

    private void LoadOrGenerateChunk(Vector3Int chunkPos)
    {

        if (!SaveSystem.LoadChunk(chunkPos, out Block[,,] blocks))
        {
            blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            TerrainGenerator.Instance.GenerateTerrain(blocks, chunkPos);
        }

        readyChunks.Enqueue((chunkPos, blocks));
        Debug.Log($"Chunk at {chunkPos} is ready for activation.");
    }

    private void ActivateChunk(Vector3Int chunkPos, Block[,,] blocks)
    {
        Chunk chunk;
        if (InactiveChunks.Count > 0)
        {
            chunk = InactiveChunks.Dequeue();
            chunk.meshFilter.sharedMesh.Clear();
        }
        else
        {
            chunk = Instantiate(chunkPrefab, transform).GetComponent<Chunk>();
        }

        chunk.position = chunkPos;
        chunk.Blocks = blocks;
        chunk.transform.position = chunkPos * Chunk.CHUNK_SIZE;

        chunk.meshCollider.sharedMesh = null;
        chunk.gameObject.SetActive(true);
        chunk.isDirty = true;

        ActiveChunks[chunkPos] = chunk;
        Debug.Log($"Activated chunk at {chunkPos}.");
    }

    private void UnloadDistantChunks(Vector3Int playerChunkCoord)
    {
        List<Vector3Int> chunksToRemove = new();

        foreach (var kvp in ActiveChunks)
        {
            Vector3Int pos = kvp.Key;
            if (Mathf.Abs(pos.x - playerChunkCoord.x) > renderDistanceHorizontal ||
                Mathf.Abs(pos.z - playerChunkCoord.z) > renderDistanceHorizontal ||
                Mathf.Abs(pos.y - playerChunkCoord.y) > renderDistanceVertical)
            {
                chunksToRemove.Add(pos);

                Chunk chunk = kvp.Value;
                chunk.meshFilter.sharedMesh.Clear();
                chunk.gameObject.SetActive(false);
                SaveSystem.SaveChunk(chunk.position, chunk.Blocks);
                InactiveChunks.Enqueue(chunk);
            }
        }

        foreach (Vector3Int key in chunksToRemove)
        {
            ActiveChunks.Remove(key);
        }
    }

    private int ManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }
}

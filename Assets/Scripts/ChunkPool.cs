using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkPool : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Transform player;
    public int viewDistance = 64;
    public int saveFile = 1;
    private readonly static int chunkSize = 16;

    public Dictionary<Vector2Int, GameObject> activeChunks = new();
    private readonly Queue<GameObject> chunkPool = new();

    async void Update()
    {
        await UpdateChunks();
    }

    async Task UpdateChunks()
    {
        // Calculate player chunk position
        Vector2 playerChunkPosition = new(player.position.x / chunkSize, player.position.z / chunkSize);
        Vector2Int playerChunkCoord = new(Mathf.FloorToInt(playerChunkPosition.x), Mathf.FloorToInt(playerChunkPosition.y));

        // Load chunks around the player
        List<Task> loadingTasks = new();
        for (int x = -viewDistance + playerChunkCoord.x; x <= viewDistance + playerChunkCoord.x; x++)
        {
            for (int y = -viewDistance + playerChunkCoord.y; y <= viewDistance + playerChunkCoord.y; y++)
            {
                Vector2Int chunkCoord = new(x, y);

                if (!activeChunks.ContainsKey(chunkCoord))
                {
                    if (Vector2Int.Distance(chunkCoord, playerChunkCoord) <= viewDistance)
                    {
                        // Start loading chunk asynchronously
                        loadingTasks.Add(LoadChunkAsync(chunkCoord));
                    }
                }
            }
        }

        // Wait for all loading tasks to complete
        await Task.WhenAll(loadingTasks);

        // Unload distant chunks
        UnloadDistantChunks(playerChunkCoord);
    }

    async Task LoadChunkAsync(Vector2Int coord)
    {
        GameObject chunk;

        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue();
        }
        else
        {
            chunk = Instantiate(chunkPrefab);
            chunk.SetActive(false);
        }

        Chunk chunkScript = chunk.GetComponent<Chunk>();
        chunkScript.x = coord.x;
        chunkScript.y = coord.y;
        activeChunks.Add(coord, chunk);

        string filePath = $"Assets/SaveData/SaveFile{saveFile}/chunk_{coord.x}_{coord.y}.dat";

        if (File.Exists(filePath))
        {
            await Task.Run(() => chunkScript.LoadData(coord)); // Load data asynchronously
        }
        else
        {
            chunkScript.voxels = null;
        }
        if (activeChunks.TryGetValue(new Vector2Int(chunkScript.x + 1, chunkScript.y), out GameObject rightChunk))
        {
            rightChunk.GetComponent<Chunk>().CreateMesh();
        }
        if (activeChunks.TryGetValue(new Vector2Int(chunkScript.x - 1, chunkScript.y), out GameObject leftChunk))
        {
            leftChunk.GetComponent<Chunk>().CreateMesh();
        }
        if (activeChunks.TryGetValue(new Vector2Int(chunkScript.x, chunkScript.y + 1), out GameObject topChunk))
        {
            topChunk.GetComponent<Chunk>().CreateMesh();
        }
        if (activeChunks.TryGetValue(new Vector2Int(chunkScript.x, chunkScript.y - 1), out GameObject bottomChunk))
        {
            bottomChunk.GetComponent<Chunk>().CreateMesh();
        }
        chunkScript.meshFilter.sharedMesh = null;
        chunk.SetActive(true);
    }

    void UnloadChunk(Vector2Int coord)
    {
        GameObject chunk = activeChunks[coord];
        Voxel[,,] chunkVoxels = chunk.GetComponent<Chunk>().voxels;
        SaveSystem.SaveChunk(coord, chunkVoxels);
        chunk.SetActive(false);
        chunkPool.Enqueue(chunk);
        activeChunks.Remove(coord);
    }

    void UnloadDistantChunks(Vector2Int playerChunkCoord)
    {
        List<Vector2Int> chunksToUnload = new();
        foreach (var chunk in activeChunks)
        {
            if (Vector2Int.Distance(chunk.Key, playerChunkCoord) > viewDistance)
            {
                chunksToUnload.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
    }
}

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
    private static readonly int chunkSize = 16;

    public Dictionary<Vector2Int, GameObject> activeChunks = new();
    private readonly Queue<GameObject> chunkPool = new();
    private readonly List<Vector2Int> chunksToUnload = new();

    void Update()
    {
        Vector2Int playerChunkCoord = GetPlayerChunkCoord();

        // Load chunks asynchronously
        for (int x = -viewDistance + playerChunkCoord.x; x <= viewDistance + playerChunkCoord.x; x++)
        {
            for (int y = -viewDistance + playerChunkCoord.y; y <= viewDistance + playerChunkCoord.y; y++)
            {
                Vector2Int chunkCoord = new(x, y);
                if (!activeChunks.ContainsKey(chunkCoord))
                {
                    _ = LoadChunkAsync(chunkCoord); // Fire and forget
                }
            }
        }

        // Unload distant chunks asynchronously
        UnloadDistantChunks(playerChunkCoord);
    }

    Vector2Int GetPlayerChunkCoord()
    {
        Vector2 playerChunkPosition = new(player.position.x / chunkSize, player.position.z / chunkSize);
        return new Vector2Int(Mathf.FloorToInt(playerChunkPosition.x), Mathf.FloorToInt(playerChunkPosition.y));
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
        activeChunks[coord] = chunk;

        string filePath = $"Assets/SaveData/SaveFile{saveFile}/chunk_{coord.x}_{coord.y}.dat";

        if (File.Exists(filePath))
        {
            Debug.Log("loading chunk from disk");
            // Load chunk data asynchronously (off the main thread)
            await Task.Run(() => chunkScript.LoadData(coord));
        }
        else
        {
            Debug.Log("generating new voxel data");
            chunkScript.voxels = null;
        }

        // Back to main thread: enable chunk and update neighbors
        UnityMainThread(() =>
        {
            UpdateNeighboringMeshes(chunkScript);
            chunkScript.meshFilter.sharedMesh = null;
            chunk.SetActive(true);
        });
    }

    void UpdateNeighboringMeshes(Chunk chunkScript)
    {
        Vector2Int[] neighbors = {
            new(chunkScript.x + 1, chunkScript.y),
            new(chunkScript.x - 1, chunkScript.y),
            new(chunkScript.x, chunkScript.y + 1),
            new(chunkScript.x, chunkScript.y - 1)
        };

        foreach (var neighbor in neighbors)
        {
            if (activeChunks.TryGetValue(neighbor, out GameObject neighborChunk))
            {
                neighborChunk.GetComponent<Chunk>().CreateMesh();
            }
        }
    }

    async Task UnloadChunkAsync(Vector2Int coord)
    {
        if (!activeChunks.TryGetValue(coord, out GameObject chunk)) return;

        Voxel[,,] chunkVoxels = chunk.GetComponent<Chunk>().voxels;

        // Save chunk data asynchronously
        await Task.Run(() => SaveSystem.SaveChunk(coord, chunkVoxels));

        // Back to main thread: deactivate chunk
        UnityMainThread(() =>
        {
            chunk.SetActive(false);
            chunkPool.Enqueue(chunk);
            activeChunks.Remove(coord);
        });
    }

    void UnloadDistantChunks(Vector2Int playerChunkCoord)
    {
        chunksToUnload.Clear();
        foreach (var chunk in activeChunks)
        {
            int xDist = Mathf.Abs(chunk.Key.x - playerChunkCoord.x);
            int yDist = Mathf.Abs(chunk.Key.y - playerChunkCoord.y);

            if (xDist > viewDistance || yDist > viewDistance)
            {
                chunksToUnload.Add(chunk.Key);
            }
        }

        foreach (var chunkCoord in chunksToUnload)
        {
            _ = UnloadChunkAsync(chunkCoord);
        }
    }

    void UnityMainThread(System.Action action)
    {
        if (action == null) return;
        if (Application.isPlaying) StartCoroutine(ExecuteOnMainThread(action));
    }

    System.Collections.IEnumerator ExecuteOnMainThread(System.Action action)
    {
        yield return null; // Wait for the next frame to ensure we are on the main thread
        action();
    }
}

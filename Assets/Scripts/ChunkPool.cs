using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkPool : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Transform player;
    public int viewDistance = 3;
    public int saveFile = 1;
    private const int CHUNK_SIZE = 16;

    public readonly Dictionary<Vector2Int, Chunk> ActiveChunks = new();
    private readonly Queue<GameObject> _chunkPool = new();
    private readonly List<Vector2Int> _chunksToUnload = new();

    private void Awake()
    {
        SaveSystem.saveDataPath = Application.persistentDataPath;
    }

    private void Start()
    {
        _ = StartLoop();
        _ = StartChunkMeshLoop();
    }

    private async Task StartChunkMeshLoop()
    {
        try
        {
            while (Application.isPlaying)
            {
                foreach (KeyValuePair<Vector2Int, Chunk> chunk in ActiveChunks)
                {
                    _ = chunk.Value.CheckMesh();
                }
                await Task.Yield(); // Prevent CPU overload
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    private async Task StartLoop()
    {
        try
        {
            while (Application.isPlaying)
            {
                await UpdateChunks();
                await Task.Delay(250); // Prevent CPU overload
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    private async Task UpdateChunks()
    {
        Vector2Int playerChunkCoord = GetPlayerChunkCoord();

        // Load chunks asynchronously
        for (int x = -viewDistance + playerChunkCoord.x; x <= viewDistance + playerChunkCoord.x; x++)
        {
            for (int y = -viewDistance + playerChunkCoord.y; y <= viewDistance + playerChunkCoord.y; y++)
            {
                Vector2Int chunkCoord = new(x, y);
                if (!ActiveChunks.ContainsKey(chunkCoord))
                {
                    await LoadChunkAsync(chunkCoord); // Await
                }
            }
        }
        // Unload distant chunks asynchronously
        UnloadDistantChunks(playerChunkCoord);
    }

    private Vector2Int GetPlayerChunkCoord()
    {
        Vector2 playerChunkPosition = new(player.position.x / CHUNK_SIZE, player.position.z / CHUNK_SIZE);
        return new(Mathf.FloorToInt(playerChunkPosition.x), Mathf.FloorToInt(playerChunkPosition.y));
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task LoadChunkAsync(Vector2Int coord)
    {
        GameObject chunk;
        if (_chunkPool.Count > 0)
        {
            chunk = _chunkPool.Dequeue();
        }
        else
        {
            chunk = Instantiate(chunkPrefab);
            chunk.SetActive(false);
        }

        Chunk chunkScript = chunk.GetComponent<Chunk>();
        chunkScript.x = coord.x;
        chunkScript.y = coord.y;
        ActiveChunks[coord] = chunkScript;

        string filePath = Path.Combine(Application.persistentDataPath, $"SaveFile{saveFile}/chunk_{coord.x}_{coord.y}.dat");

        if (File.Exists(filePath))
        {
            // Load chunk data asynchronously
            await chunkScript.LoadData(coord);
        }
        else
        {
            chunkScript.Voxels = chunkScript.terrainGenerator.GenerateTerrain(coord.x, coord.y);
        }

        // Back to main thread: enable chunk and update neighbors
        UnityMainThread(() =>
        {
            UpdateNeighboringMeshes(chunkScript);
            chunkScript.meshRenderer.enabled = false;
            chunk.SetActive(true);
        });
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void UpdateNeighboringMeshes(Chunk chunkScript)
    {
        Vector2Int[] neighbors = {
            new(chunkScript.x + 1, chunkScript.y),
            new(chunkScript.x - 1, chunkScript.y),
            new(chunkScript.x, chunkScript.y + 1),
            new(chunkScript.x, chunkScript.y - 1)
        };

        foreach (Vector2Int neighbor in neighbors)
        {
            if (ActiveChunks.TryGetValue(neighbor, out Chunk neighborChunk))
            {
                neighborChunk.isDirty = true;
            }
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task UnloadChunkAsync(Vector2Int coord)
    {
        if (!ActiveChunks.TryGetValue(coord, out Chunk chunkScript))
        {
            return;
        }

        Voxel[,,] chunkVoxels = chunkScript.Voxels;

        // Save chunk data asynchronously
        await Task.Run(() => SaveSystem.SaveChunk(coord, chunkVoxels));

        // Back to main thread: deactivate chunk
        UnityMainThread(() =>
        {
            GameObject chunk = chunkScript.gameObject;
            chunk.SetActive(false);
            _chunkPool.Enqueue(chunk);
            _ = ActiveChunks.Remove(coord);
        });
    }

    private void UnloadDistantChunks(Vector2Int playerChunkCoord)
    {
        _chunksToUnload.Clear();
        foreach (KeyValuePair<Vector2Int, Chunk> chunk in from chunk in ActiveChunks
                                                          let xDist = Mathf.Abs(chunk.Key.x - playerChunkCoord.x)
                                                          let yDist = Mathf.Abs(chunk.Key.y - playerChunkCoord.y)
                                                          where xDist > viewDistance || yDist > viewDistance
                                                          select chunk)
        {
            _chunksToUnload.Add(chunk.Key);
        }

        foreach (Vector2Int chunkCoord in _chunksToUnload)
        {
            _ = UnloadChunkAsync(chunkCoord);
        }
    }

    private void UnityMainThread(Action action)
    {
        if (action == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            _ = StartCoroutine(ExecuteOnMainThread(action));
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private static IEnumerator ExecuteOnMainThread(Action action)
    {
        yield return null; // Wait for the next frame to ensure we are on the main thread
        action();
    }
}

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

    public readonly Dictionary<Vector3Int, Chunk> ActiveChunks = new();
    private readonly Queue<GameObject> _chunkPool = new();
    private readonly List<Vector3Int> _chunksToUnload = new();

    // Singleton
    public static ChunkPool Instance { get; private set; }

    private void Awake()
    {
        SaveSystem.saveDataPath = Application.persistentDataPath;
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _ = StartLoop();
        _ = StartChunkMeshLoop();
    }

    private async Task StartChunkMeshLoop()
    {

        while (Application.isPlaying)
        {
            foreach (KeyValuePair<Vector3Int, Chunk> chunk in ActiveChunks)
            {
                _ = chunk.Value.CheckMesh();
            }
            await Task.Yield(); // Prevent CPU overload
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
        Vector3Int playerChunkCoord = GetPlayerChunkCoord();

        // Load chunks asynchronously
        for (int x = -viewDistance + playerChunkCoord.x; x <= viewDistance + playerChunkCoord.x; x++)
        {
            for (int y = -viewDistance + playerChunkCoord.y; y <= viewDistance + playerChunkCoord.y; y++)
            {
                for (int z = -viewDistance + playerChunkCoord.z; z <= viewDistance + playerChunkCoord.z; z++)
                {
                    Vector3Int chunkCoord = new(x, y, z);
                    if (!ActiveChunks.ContainsKey(chunkCoord))
                    {
                        await LoadChunkAsync(chunkCoord); // Await
                    }
                }
            }
        }
        // Unload distant chunks asynchronously
        UnloadDistantChunks(playerChunkCoord);
    }

    private Vector3Int GetPlayerChunkCoord()
    {
        Vector3 playerChunkPosition = player.position / CHUNK_SIZE;
        return new Vector3Int(
            Mathf.FloorToInt(playerChunkPosition.x),
            Mathf.FloorToInt(playerChunkPosition.y),  // Use y-axis
            Mathf.FloorToInt(playerChunkPosition.z)   // Use z-axis
        );
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task LoadChunkAsync(Vector3Int coord)
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
        chunkScript.position = new Vector3Int(coord.x, coord.y, coord.z); // Updated with z

        ActiveChunks[coord] = chunkScript;

        string filePath = Path.Combine(Application.persistentDataPath, $"SaveFile{saveFile}/chunk_{coord.x}_{coord.y}_{coord.z}.dat"); // Use z in the file path

        if (File.Exists(filePath))
        {
            // Load chunk data asynchronously
            await chunkScript.LoadData(coord);
        }
        else
        {
            chunkScript.Voxels = TerrainGenerator.Instance.GenerateTerrain(coord.x, coord.z);
        }

        UpdateNeighboringMeshes(chunkScript);
        chunkScript.meshRenderer.enabled = false;
        chunk.SetActive(true);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void UpdateNeighboringMeshes(Chunk chunkScript)
    {
        Vector3Int[] neighbors = {
        new(chunkScript.position.x + 1, chunkScript.position.y, chunkScript.position.z),
        new(chunkScript.position.x - 1, chunkScript.position.y, chunkScript.position.z),
        new(chunkScript.position.x, chunkScript.position.y + 1, chunkScript.position.z),
        new(chunkScript.position.x, chunkScript.position.y - 1, chunkScript.position.z),
        new(chunkScript.position.x, chunkScript.position.y, chunkScript.position.z + 1), // Add z-axis
        new(chunkScript.position.x, chunkScript.position.y, chunkScript.position.z - 1)  // Add z-axis
        };

        foreach (Vector3Int neighbor in neighbors)
        {
            if (ActiveChunks.TryGetValue(neighbor, out Chunk neighborChunk))
            {
                neighborChunk.IsDirty = true;
            }
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task UnloadChunkAsync(Vector3Int coord)
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

    private void UnloadDistantChunks(Vector3Int playerChunkCoord)
    {
        _chunksToUnload.Clear();
        foreach (KeyValuePair<Vector3Int, Chunk> chunk in from chunk in ActiveChunks
                                                          let xDist = Mathf.Abs(chunk.Key.x - playerChunkCoord.x)
                                                          let yDist = Mathf.Abs(chunk.Key.y - playerChunkCoord.y)
                                                          let zDist = Mathf.Abs(chunk.Key.z - playerChunkCoord.z)
                                                          where xDist > viewDistance || yDist > viewDistance || zDist > viewDistance
                                                          select chunk)
        {
            _chunksToUnload.Add(chunk.Key);
        }

        foreach (Vector3Int chunkCoord in _chunksToUnload)
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

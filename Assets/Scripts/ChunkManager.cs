using System;
using System.Collections;
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
    }
    /// <summary>
    /// How far to render chunks from the player.
    /// </summary>
    public int renderDistance = 3;

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
    void Start()
    {
        _ = StartUpdateLoop(); // Start the update loop asynchronously
    }

    private void OnApplicationQuit()
    {
        running = false; // Stop the update loop when the application quits
    }

    public bool running = true;
    private async Task StartUpdateLoop()
    {
        while (running)
        {
            UpdateChunks();
            await Task.Yield(); // Yield to allow other tasks to run
        }
    }

    // Update is called once per frame.
    void UpdateChunks()
    {
        UnityMainThread(() =>
        {
            var playerChunkCoord = Vector3Int.FloorToInt(Player.Instance.transform.position / Chunk.CHUNK_SIZE);

            // Activation: Ensure chunks within render distance are active.
            for (int x = playerChunkCoord.x - renderDistance; x <= playerChunkCoord.x + renderDistance; x++)
            {
                for (int y = playerChunkCoord.y - renderDistance; y <= playerChunkCoord.y + renderDistance; y++)
                {
                    for (int z = playerChunkCoord.z - renderDistance; z <= playerChunkCoord.z + renderDistance; z++)
                    {
                        Vector3Int chunkPos = new(x, y, z);
                        if (!ActiveChunks.ContainsKey(chunkPos))
                        {
                            Chunk chunk;
                            if (InactiveChunks.Count > 0)
                            {
                                // Reuse chunk from the inactive pool.
                                chunk = InactiveChunks.Dequeue();
                                chunk.meshFilter.sharedMesh.Clear();
                            }
                            else
                            {
                                // Instantiate a new chunk.
                                chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                            }

                            chunk.position = chunkPos;
                            chunk.transform.position = chunk.position * Chunk.CHUNK_SIZE;
                            TerrainGenerator.Instance.GenerateTerrain(chunk.Blocks, chunk.position);
                            ActiveChunks.Add(chunkPos, chunk);
                            chunk.meshCollider.sharedMesh = null;
                            chunk.gameObject.SetActive(true);
                            chunk.isDirty = true;
                        }
                    }
                }
            }

            // Deactivation: Remove chunks outside the render distance.
            var chunksToRemove = new List<Vector3Int>();
            foreach (var kvp in ActiveChunks)
            {
                var chunk = kvp.Value;
                var pos = kvp.Key;
                if (pos.x > playerChunkCoord.x + renderDistance || pos.x < playerChunkCoord.x - renderDistance ||
                    pos.y > playerChunkCoord.y + renderDistance || pos.y < playerChunkCoord.y - renderDistance ||
                    pos.z > playerChunkCoord.z + renderDistance || pos.z < playerChunkCoord.z - renderDistance)
                {
                    chunksToRemove.Add(pos);
                    chunk.meshFilter.sharedMesh.Clear();
                    chunk.gameObject.SetActive(false);
                    InactiveChunks.Enqueue(chunk);
                }
            }

            foreach (var key in chunksToRemove)
            {
                ActiveChunks.Remove(key);
            }
        });
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

    private static IEnumerator ExecuteOnMainThread(Action action)
    {
        yield return null; // Wait for the next frame to ensure we are on the main thread
        action();
    }
}

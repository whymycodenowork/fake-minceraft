using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }

    public int renderDistance = 3;
    public GameObject chunkPrefab;

    public Dictionary<Vector3Int, Chunk> ActiveChunks = new();
    public Queue<Chunk> InactiveChunks = new();
    public Dictionary<Vector3Int, bool> PendingChunks = new();

    private Transform playerTransform;
    private Vector3Int lastPlayerChunk;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        TextureManager.CreateTextures();
        SaveSystem.saveDataPath = Application.persistentDataPath;
    }

    private void Start()
    {
        playerTransform = Player.Instance.transform;
        lastPlayerChunk = GetPlayerChunkCoord();
    }

    private void Update()
    {
        Vector3Int currentChunk = GetPlayerChunkCoord();
        if (currentChunk != lastPlayerChunk)
        {
            lastPlayerChunk = currentChunk;
            UpdateChunks();
        }
    }

    private Vector3Int GetPlayerChunkCoord()
    {
        return Vector3Int.FloorToInt(playerTransform.position / Chunk.CHUNK_SIZE);
    }

    private void UpdateChunks()
    {
        Vector3Int playerChunk = lastPlayerChunk;
        HashSet<Vector3Int> desiredChunks = new();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int pos = playerChunk + new Vector3Int(x, y, z);
                    _ = desiredChunks.Add(pos);
                    if (!ActiveChunks.ContainsKey(pos) && !PendingChunks.ContainsKey(pos))
                    {
                        PrepareChunk(pos);
                    }
                }
            }
        }

        List<Vector3Int> keysToRemove = new();
        foreach (KeyValuePair<Vector3Int, Chunk> kvp in ActiveChunks)
        {
            if (!desiredChunks.Contains(kvp.Key))
            {
                DeactivateChunk(kvp.Key, kvp.Value);
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (Vector3Int key in keysToRemove)
        {
            _ = ActiveChunks.Remove(key);
        }
    }

    private void ActivateChunk(Vector3Int pos, Block[,,] blocks)
    {
        Debug.Log($"Activating Chunk.");
        Chunk chunk;
        if (InactiveChunks.Count > 0)
        {
            chunk = InactiveChunks.Dequeue();
            chunk.meshFilter.sharedMesh.Clear();
        }
        else
        {
            chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
        }

        chunk.position = pos;
        chunk.Blocks = blocks;
        chunk.transform.position = pos * Chunk.CHUNK_SIZE;

        chunk.meshCollider.sharedMesh = null;
        chunk.gameObject.SetActive(true);
        chunk.isDirty = true;

        ActiveChunks[pos] = chunk;
    }

    private void PrepareChunk(Vector3Int pos)
    {
        Debug.Log($"Preparing Chunk.");
        PendingChunks[pos] = false;

        _ = Task.Run(() =>
        {
            Block[,,] blocks;
            try
            {
                if (!SaveSystem.TryLoadChunk(pos, out blocks))
                {
                    blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
                    TerrainGenerator.Instance.GenerateTerrain(blocks, pos);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading chunk at {pos}: {e.Message}");
                blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];
            }

            MainThread(() =>
            {
                ActivateChunk(pos, blocks);
                _ = PendingChunks.Remove(pos);
            });
        });
    }

    private void DeactivateChunk(Vector3Int pos, Chunk chunk)
    {
        Debug.Log($"Deactivating Chunk.");
        SaveSystem.SaveChunk(pos, chunk.Blocks);
        chunk.meshFilter.sharedMesh.Clear();
        chunk.gameObject.SetActive(false);
        InactiveChunks.Enqueue(chunk);
    }

    private void MainThread(Action action)
    {
        _ = StartCoroutine(WaitForEndOfFrame(action));
    }

    private IEnumerator WaitForEndOfFrame(Action action)
    {
        yield return null;
        action();
    }
}

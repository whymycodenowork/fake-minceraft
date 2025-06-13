using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
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
        
    }

    // Update is called once per frame
    void Update()
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(x, y, z);
                    if (!ActiveChunks.ContainsKey(chunkPos))
                    {
                        if (InactiveChunks.Count <= 0)
                        {
                            Chunk chunk = Instantiate(chunkPrefab).GetComponent<Chunk>();
                            chunk.position = chunkPos;
                            chunk.gameObject.SetActive(true);
                            ActiveChunks.Add(chunkPos, chunk);
                        }
                        else
                        {
                            var chunk = InactiveChunks.Dequeue();
                            chunk.position = chunkPos;
                            chunk.gameObject.SetActive(true);
                            ActiveChunks.Add(chunkPos, chunk);
                        }
                    }
                }
            }
        }
        foreach (var chunk in ActiveChunks.Values)
        {
            if (chunk.position.x > renderDistance || chunk.position.x < -renderDistance ||
                chunk.position.y > renderDistance || chunk.position.y < -renderDistance ||
                chunk.position.z > renderDistance || chunk.position.z < -renderDistance)
            {
                // If the chunk is outside the render distance, mark it as inactive
                InactiveChunks.Enqueue(chunk);
                ActiveChunks.Remove(chunk.position);
                chunk.gameObject.SetActive(false);
            }
        }
    }
}

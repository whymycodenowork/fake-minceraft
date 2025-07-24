using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly ConcurrentDictionary<Vector3Int, Block[,,]> chunkCache = new();
    public static float saveInterval = 300f; // Save to disk every once in a while
    private static float saveTimer;

    public static string saveDataPath; // Needs to be initialized elsewhere

    private static string GetSaveFilePath(Vector3Int coord)
    {
        string saveFolderPath = Path.Combine(saveDataPath, "SaveFile1");
        if (!Directory.Exists(saveFolderPath))
        {
            _ = Directory.CreateDirectory(saveFolderPath);
        }
        return Path.Combine(saveFolderPath, $"chunk_{coord.x}_{coord.y}_{coord.z}.dat");
    }

    // Save chunk to disk
    private static void SaveChunkToDisk(string path, Block[,,] voxels)
    {
        if (voxels == null)
        {
            return;
        }

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
        using BinaryWriter writer = new(stream);

        for (int i = 0; i < Chunk.CHUNK_SIZE; i++)
        {
            for (int j = 0; j < Chunk.CHUNK_SIZE; j++)
            {
                for (int k = 0; k < Chunk.CHUNK_SIZE; k++)
                {
                    writer.Write(voxels[i, j, k].data);
                }
            }
        }
    }

    // Load chunk from disk
    private static void LoadChunkFromDisk(string path, out Block[,,] blocks)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        using BinaryReader reader = new(stream);

        blocks = new Block[Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE, Chunk.CHUNK_SIZE];

        for (int i = 0; i < Chunk.CHUNK_SIZE; i++)
        {
            for (int j = 0; j < Chunk.CHUNK_SIZE; j++)
            {
                for (int k = 0; k < Chunk.CHUNK_SIZE; k++)
                {
                    blocks[i, j, k] = new(reader.ReadUInt64());
                }
            }
        }
    }

    // Save chunk to RAM
    public static void SaveChunk(Vector3Int coord, Block[,,] blocks)
    {
        if (blocks == null)
        {
            Debug.LogWarning("Trying to save a null chunk");
            return;
        }
        chunkCache[coord] = blocks;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="coord">The coordinates of the chunk</param>
    /// <param name="blocks">The blocks of the chunk</param>
    /// <returns>True if the chunk was loaded successfully, false if not</returns>
    public static bool LoadChunk(Vector3Int coord, out Block[,,] blocks)
    {
        if (chunkCache.TryGetValue(coord, out blocks))
        {
            return true;
        }

        string filePath = GetSaveFilePath(coord);
        if (File.Exists(filePath))
        {
            LoadChunkFromDisk(filePath, out blocks);
            return true;
        }
        return false;
    }

    // Save all cached chunks to disk
    public static void SaveAllChunksToDisk()
    {
        foreach (KeyValuePair<Vector3Int, Block[,,]> kvp in chunkCache)
        {
            string path = GetSaveFilePath(kvp.Key);
            SaveChunkToDisk(path, kvp.Value);
        }
        chunkCache.Clear();
    }

    // Update method for periodic saving
    public static void Update(float deltaTime)
    {
        saveTimer += deltaTime;

        if (!(saveTimer >= saveInterval))
        {
            return;
        }

        SaveAllChunksToDisk();
        saveTimer = 0f;
    }
}
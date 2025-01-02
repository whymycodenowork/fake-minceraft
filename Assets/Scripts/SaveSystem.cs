using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly Dictionary<Vector2Int, Voxel[,,]> ChunkCache = new Dictionary<Vector2Int, Voxel[,,]>();
    private static float saveInterval = 300f; // Save to disk every 5 minutes (300 seconds)
    private static float saveTimer = 0f;

    // Save chunk to disk
    public static void SaveChunkToDisk(string path, Voxel[,,] voxels)
    {
        if (voxels == null)
        {
            Debug.LogWarning("voxels is null!");
            return;
        }

        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write voxel data
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 255; j++)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            writer.Write(voxels[i, j, k].id);
                            writer.Write(voxels[i, j, k].type);
                        }
                    }
                }
            }
        }
    }

    // Load chunk from disk
    public static void LoadChunkFromDisk(string path, out Voxel[,,] voxels)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Initialize voxel array
                voxels = new Voxel[16, 255, 16];

                // Read voxel data
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 255; j++)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            byte id = reader.ReadByte();
                            byte type = reader.ReadByte();
                            voxels[i, j, k] = new Voxel(id, type);
                        }
                    }
                }
            }
        }
    }

    // Save chunk to RAM
    public static void SaveChunk(Vector2Int coord, Voxel[,,] voxels)
    {
        if (voxels == null)
        {
            Debug.LogWarning("Trying to save a null chunk");
            return;
        }

        if (ChunkCache.ContainsKey(coord))
        {
            ChunkCache[coord] = voxels;
        }
        else
        {
            ChunkCache.Add(coord, voxels);
        }
    }

    // Load chunk
    public static bool LoadChunk(Vector2Int coord, out Voxel[,,] voxels)
    {
        if (ChunkCache.TryGetValue(coord, out voxels))
        {
            return true;
        }
        // 1 for now
        else if (File.Exists($"Assets/SaveData/SaveFile{1}/chunk_{coord.x}_{coord.y}.dat"))
        {
            LoadChunkFromDisk($"Assets/SaveData/SaveFile{1}/chunk_{coord.x}_{coord.y}.dat", out voxels);
            return true;
        }
        Debug.LogWarning($"Chunk at {coord} not found");
        return false;
    }

    // Save all cached chunks to disk
    public static void SaveAllChunksToDisk()
    {
        foreach (var kvp in ChunkCache)
        {
            string path = $"Assets/SaveData/SaveFile{1}/chunk_{kvp.Key.x}_{kvp.Key.y}.dat";  
            SaveChunkToDisk(path, kvp.Value);
        }
        ChunkCache.Clear();
        Debug.Log("All chunks saved to disk.");
    }

    // Update method for periodic saving
    public static void Update(float deltaTime)
    {
        saveTimer += deltaTime;

        if (saveTimer >= saveInterval)
        {
            SaveAllChunksToDisk();
            saveTimer = 0f;
        }
    }
}

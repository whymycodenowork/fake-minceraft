using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static readonly Dictionary<Vector2Int, Voxel[,,]> chunkCache = new();
    public static float saveInterval = 300f; // Save to disk every once in a while
    private static float _saveTimer;

    public static string saveDataPath; // Needs to be initialized elsewhere

    private static string GetSaveFilePath(Vector2Int coord)
    {
        var saveFolderPath = Path.Combine(saveDataPath, "SaveFile1");
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
        return Path.Combine(saveFolderPath, $"chunk_{coord.x}_{coord.y}.dat");
    }

    // Save chunk to disk
    private static void SaveChunkToDisk(string path, Voxel[,,] voxels)
    {
        if (voxels == null)
        {
            return;
        }

        using FileStream stream = new(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096);
        using BinaryWriter writer = new(stream);
        
        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 255; j++)
            {
                for (var k = 0; k < 16; k++)
                {
                    writer.Write(voxels[i, j, k].ID);
                    writer.Write(voxels[i, j, k].Type);
                }
            }
        }
    }

    // Load chunk from disk
    private static void LoadChunkFromDisk(string path, out Voxel[,,] voxels)
    {
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        using BinaryReader reader = new(stream);
        
        voxels = new Voxel[16, 255, 16];

        for (var i = 0; i < 16; i++)
        {
            for (var j = 0; j < 255; j++)
            {
                for (var k = 0; k < 16; k++)
                {
                    var id = reader.ReadByte();
                    var type = reader.ReadByte();
                    voxels[i, j, k] = new(id, type);
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
        chunkCache[coord] = voxels;
    }

    // Load chunk
    public static void LoadChunk(Vector2Int coord, out Voxel[,,] voxels)
    {
        if (chunkCache.TryGetValue(coord, out voxels))
        {
            return;
        }

        var filePath = GetSaveFilePath(coord);
        if (File.Exists(filePath))
        {
            LoadChunkFromDisk(filePath, out voxels);
            return;
        }
        Debug.LogWarning($"Chunk at {coord} not found");
    }

    // Save all cached chunks to disk
    public static void SaveAllChunksToDisk()
    {
        foreach (var kvp in chunkCache)
        {
            var path = GetSaveFilePath(kvp.Key);
            SaveChunkToDisk(path, kvp.Value);
        }
        chunkCache.Clear();
    }

    // Update method for periodic saving
    // ReSharper disable Unity.PerformanceAnalysis
    public static void Update(float deltaTime)
    {
        _saveTimer += deltaTime;

        if (!(_saveTimer >= saveInterval)) return;
        SaveAllChunksToDisk();
        _saveTimer = 0f;
    }
}

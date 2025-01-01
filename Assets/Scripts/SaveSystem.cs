using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static void SaveChunkToDisk(string path, int x, int y, Voxel[,,] voxels)
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
                // Write x and y
                writer.Write(x);
                writer.Write(y);

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

    public static void LoadChunkFromDisk(string path, ref int x, ref int y, ref Voxel[,,] voxels)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                // Read x and y
                x = reader.ReadInt32();
                y = reader.ReadInt32();

                // Initialize voxel array
                voxels = new Voxel[16, 255, 16];

                // Read voxel data
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 255; j++)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            voxels[i, j, k] = new Voxel();
                            voxels[i, j, k].id = reader.ReadByte();
                            voxels[i, j, k].type = reader.ReadByte();
                        }
                    }
                }
            }
        }
    }

    public static void SaveChunk(Vector2Int coord, Voxel[,,] voxels)
    {

    }
}
